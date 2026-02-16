using System.Runtime.InteropServices;
using MFilesAPI;
using Microsoft.Extensions.Logging;

namespace Taskify.Connectors.MFiles;

/// <summary>
/// All M-Files COM API knowledge lives here and ONLY here.
/// When M-Files goes away, you delete this folder. Nothing else changes.
/// </summary>
public class MFilesConnector : ITaskDataSource, IDisposable
{
    private readonly MFilesConfiguration _config;
    private readonly ILogger _logger;
    private MFilesClientApplication? _mfilesClientApp;
    private Vault? _vault;
    private bool _isConnected;
    private readonly object _connectionLock = new();
    private bool _disposed;

    public MFilesConnector(MFilesConfiguration config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _mfilesClientApp = new MFilesClientApplication();

        Connect();
    }

    public Task<IReadOnlyList<TaskDTO>> GetAllTasksAsync()
    {
        var vault = GetVault();

        var searchConditions = new SearchConditions();

        // Not deleted
        var deletedCondition = new SearchCondition
        {
            ConditionType = MFConditionType.MFConditionTypeEqual
        };
        deletedCondition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeDeleted);
        deletedCondition.TypedValue.SetValue(MFDataType.MFDatatypeBoolean, false);
        searchConditions.Add(-1, deletedCondition);

        // Object type = Assignment
        var objectTypeCondition = new SearchCondition
        {
            ConditionType = MFConditionType.MFConditionTypeEqual
        };
        objectTypeCondition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectTypeID);
        objectTypeCondition.TypedValue.SetValue(
            MFDataType.MFDatatypeLookup,
            (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment);
        searchConditions.Add(-1, objectTypeCondition);

        // Assigned to current user
        var assignedToCondition = new SearchCondition
        {
            ConditionType = MFConditionType.MFConditionTypeEqual
        };
        assignedToCondition.Expression.SetPropertyValueExpression(
            (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefAssignedTo,
            MFParentChildBehavior.MFParentChildBehaviorNone);
        assignedToCondition.TypedValue.SetValue(
            MFDataType.MFDatatypeLookup,
            vault.CurrentLoggedInUserID);
        searchConditions.Add(-1, assignedToCondition);

        var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditions(
            searchConditions,
            MFSearchFlags.MFSearchFlagNone,
            SortResults: false);

        var tasks = MFilesTaskMapper.MapAll(vault, searchResults);
        return Task.FromResult(tasks);
    }

    public Task<TaskDTO?> GetTaskByIdAsync(string taskId)
    {
        if (!int.TryParse(taskId, out var id))
            return Task.FromResult<TaskDTO?>(null);

        var vault = GetVault();

        var objID = new ObjID();
        objID.SetIDs(
            ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment,
            ID: id);

        var objVersion = vault.ObjectOperations.GetLatestObjectVersionAndProperties(objID, AllowCheckedOut: true);
        var task = MFilesTaskMapper.Map(vault, objVersion.VersionData);

        return Task.FromResult<TaskDTO?>(task);
    }

    public Task<IReadOnlyList<TaskDTO>> GetTasksByAssigneeAsync(string assigneeId)
    {
        // M-Files connector returns tasks for the current logged-in user only
        return GetAllTasksAsync();
    }

    public Task<bool> UpdateTaskStatusAsync(string taskId, TaskItemStatus newStatus)
    {
        if (!int.TryParse(taskId, out var id))
            return Task.FromResult(false);

        if (newStatus != TaskItemStatus.Completed)
            throw new NotSupportedException("M-Files connector only supports marking assignments as complete.");

        var vault = GetVault();

        var objVer = new ObjVer();
        objVer.SetIDs(
            ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment,
            ID: id,
            Version: -1);

        vault.ObjectPropertyOperations.MarkAssignmentComplete(objVer);
        return Task.FromResult(true);
    }

    public Task<bool> IsAvailableAsync()
    {
        try
        {
            var vault = GetVault();
            return Task.FromResult(vault != null);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<string> GetCurrentUserNameAsync()
    {
        var vault = GetVault();

        try
        {
            var userLookupValue = new TypedValue();
            userLookupValue.SetValue(MFDataType.MFDatatypeLookup, vault.CurrentLoggedInUserID);
            var displayValue = userLookupValue.DisplayValue;
            if (!string.IsNullOrWhiteSpace(displayValue))
                return Task.FromResult(displayValue);
        }
        catch { }

        try
        {
            var sessionInfo = vault.SessionInfo;
            if (sessionInfo != null && !string.IsNullOrWhiteSpace(sessionInfo.AccountName))
                return Task.FromResult(sessionInfo.AccountName);
        }
        catch { }

        return Task.FromResult($"User_{vault.CurrentLoggedInUserID}");
    }

    private void Connect()
    {
        lock (_connectionLock)
        {
            if (_isConnected) return;

            try
            {
                _vault = _mfilesClientApp?.GetVaultConnectionsWithGUID(_config.VaultGuid)
                    .Cast<VaultConnection>()
                    .FirstOrDefault()?
                    .BindToVault(IntPtr.Zero, CanLogIn: true, ReturnNULLIfCancelledByUser: true);

                if (_vault == null)
                    throw new InvalidOperationException(
                        $"Failed to bind to vault {_config.VaultGuid}. Vault may not exist or login was cancelled.");

                _isConnected = true;
                _logger.LogInformation("Successfully connected to M-Files vault");
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _vault = null;
                _logger.LogError(ex, "Failed to connect to M-Files vault {VaultGuid}", _config.VaultGuid);
                throw new ApplicationException($"Failed to connect to M-Files vault {_config.VaultGuid}", ex);
            }
        }
    }

    private Vault GetVault()
    {
        lock (_connectionLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_isConnected || _vault == null)
                throw new InvalidOperationException("M-Files vault is not connected.");

            return _vault;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_connectionLock)
        {
            if (_vault != null)
            {
                Marshal.ReleaseComObject(_vault);
                _vault = null;
            }

            if (_mfilesClientApp != null)
            {
                Marshal.ReleaseComObject(_mfilesClientApp);
                _mfilesClientApp = null;
            }

            _isConnected = false;
            _disposed = true;
        }
    }
}
