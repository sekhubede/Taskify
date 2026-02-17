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
    private string? _cachedUserDisplayName;

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
        if (_cachedUserDisplayName != null)
            return Task.FromResult(_cachedUserDisplayName);

        var vault = GetVault();

        // Resolve display name from the built-in Users value list.
        // This returns the same name used by version history's LastModifiedBy.DisplayValue,
        // ensuring the current user name matches comment author names.
        try
        {
            var userItem = vault.ValueListItemOperations.GetValueListItemByID(
                (int)MFBuiltInValueList.MFBuiltInValueListUsers,
                vault.CurrentLoggedInUserID);

            if (!string.IsNullOrWhiteSpace(userItem.Name))
            {
                _cachedUserDisplayName = userItem.Name;
                return Task.FromResult(_cachedUserDisplayName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve user display name from value list");
        }

        // Fallback: session account name
        try
        {
            var sessionInfo = vault.SessionInfo;
            if (sessionInfo != null && !string.IsNullOrWhiteSpace(sessionInfo.AccountName))
            {
                _cachedUserDisplayName = sessionInfo.AccountName;
                return Task.FromResult(_cachedUserDisplayName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve user name from session info");
        }

        var fallback = $"User_{vault.CurrentLoggedInUserID}";
        _cachedUserDisplayName = fallback;
        return Task.FromResult(fallback);
    }

    // ── Comments ──

    public Task<IReadOnlyList<CommentDTO>> GetCommentsForTaskAsync(string taskId)
    {
        if (!int.TryParse(taskId, out var assignmentId))
            return Task.FromResult<IReadOnlyList<CommentDTO>>(new List<CommentDTO>());

        try
        {
            var vault = GetVault();
            var comments = new List<CommentDTO>();

            var objID = new ObjID();
            objID.SetIDs(
                ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment,
                ID: assignmentId);

            // Read version comments by iterating versions from latest to first
            try
            {
                var latest = vault.ObjectOperations.GetLatestObjVer(objID, AllowCheckedOut: true);
                for (int v = latest.Version; v >= 1; v--)
                {
                    var objVer = new ObjVer();
                    objVer.SetIDs((int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment, assignmentId, v);

                    try
                    {
                        var vc = vault.ObjectPropertyOperations.GetVersionComment(objVer);
                        if (vc != null)
                        {
                            var commentContent = vc.VersionComment.Value?.Value?.ToString()
                                ?? vc.VersionComment.Value?.DisplayValue
                                ?? string.Empty;

                            if (!string.IsNullOrWhiteSpace(commentContent))
                            {
                                comments.Add(new CommentDTO
                                {
                                    Id = vc.ObjVer.Version,
                                    Content = commentContent,
                                    AuthorName = vc.LastModifiedBy.Value.DisplayValue,
                                    CreatedDate = (DateTime)vc.StatusChanged.Value.GetValueAsTimestamp().UtcToLocalTime().GetValue(),
                                    AssignmentId = assignmentId
                                });
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // Fallback: parse multi-line comment property if no version comments found
            if (comments.Count == 0)
            {
                var latestObjVer = vault.ObjectOperations.GetLatestObjVer(objID, AllowCheckedOut: true);
                var propertyValues = vault.ObjectPropertyOperations.GetProperties(latestObjVer);

                if (propertyValues.IndexOf((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefComment) != -1)
                {
                    var commentProperty = propertyValues.SearchForProperty(
                        (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefComment);

                    if (commentProperty != null && !commentProperty.TypedValue.IsNULL())
                    {
                        var commentText = commentProperty.TypedValue.Value?.ToString() ?? string.Empty;

                        if (!string.IsNullOrWhiteSpace(commentText))
                        {
                            var commentLines = System.Text.RegularExpressions.Regex.Split(
                                commentText,
                                "(?:\r?\n){2,}",
                                System.Text.RegularExpressions.RegexOptions.Singleline)
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .ToArray();

                            int commentId = 1;
                            foreach (var line in commentLines)
                            {
                                var parsed = ParseCommentLine(line, assignmentId, commentId);
                                if (parsed != null)
                                {
                                    comments.Add(parsed);
                                    commentId++;
                                }
                            }
                        }
                    }
                }
            }

            var sorted = comments.OrderBy(c => c.Id).ToList();
            return Task.FromResult<IReadOnlyList<CommentDTO>>(sorted);
        }
        catch (Exception ex)
        {
            throw new ApplicationException(
                $"Failed to retrieve comments for assignment {assignmentId}", ex);
        }
    }

    public Task<CommentDTO> AddCommentAsync(string taskId, string content)
    {
        if (!int.TryParse(taskId, out var assignmentId))
            throw new ArgumentException("Invalid task ID", nameof(taskId));

        var vault = GetVault();

        var objID = new ObjID();
        objID.SetIDs(
            ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment,
            ID: assignmentId);

        try
        {
            var checkedOutVersion = vault.ObjectOperations.CheckOut(objID);
            var propertyValues = vault.ObjectPropertyOperations.GetProperties(checkedOutVersion.ObjVer);

            AppendToMultiLineTextProperty(
                propertyValues,
                (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefComment,
                content);

            vault.ObjectPropertyOperations.SetAllProperties(
                checkedOutVersion.ObjVer,
                AllowModifyingCheckedInObject: true,
                propertyValues);
            vault.ObjectOperations.CheckIn(checkedOutVersion.ObjVer);

            var pseudoId = (int)(DateTime.UtcNow.Ticks % int.MaxValue);
            var author = GetCurrentUserNameAsync().GetAwaiter().GetResult();

            var result = new CommentDTO
            {
                Id = pseudoId,
                Content = content,
                AuthorName = author,
                CreatedDate = DateTime.UtcNow,
                AssignmentId = assignmentId
            };

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            throw new ApplicationException(
                $"Failed to add comment to assignment {assignmentId}", ex);
        }
    }

    public Task<int> GetCommentCountAsync(string taskId)
    {
        try
        {
            var comments = GetCommentsForTaskAsync(taskId).GetAwaiter().GetResult();
            return Task.FromResult(comments.Count);
        }
        catch
        {
            return Task.FromResult(0);
        }
    }

    // ── Comment Helpers ──

    private static CommentDTO? ParseCommentLine(string line, int assignmentId, int commentId)
    {
        try
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                line,
                @"\[([\d\-\s:]+)\]\s+([^:]+):\s+(.+)",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            if (match.Success)
            {
                return new CommentDTO
                {
                    Id = commentId,
                    Content = match.Groups[3].Value.Trim(),
                    AuthorName = match.Groups[2].Value.Trim(),
                    CreatedDate = DateTime.Parse(match.Groups[1].Value),
                    AssignmentId = assignmentId
                };
            }

            return new CommentDTO
            {
                Id = commentId,
                Content = line,
                AuthorName = "Unknown",
                CreatedDate = DateTime.UtcNow,
                AssignmentId = assignmentId
            };
        }
        catch
        {
            return null;
        }
    }

    private static void AppendToMultiLineTextProperty(
        PropertyValues properties,
        int propertyDef,
        string textToAppend,
        string separator = "\n\n")
    {
        var propertyValue = new PropertyValue { PropertyDef = propertyDef };

        if (properties.IndexOf(propertyDef) == -1)
            properties.Add(-1, propertyValue);

        var existingProperty = properties.SearchForProperty(propertyDef);

        string existingText = string.Empty;
        if (existingProperty != null && !existingProperty.TypedValue.IsNULL())
            existingText = existingProperty.TypedValue.Value?.ToString() ?? string.Empty;

        var newText = string.IsNullOrEmpty(existingText)
            ? textToAppend
            : $"{existingText}{separator}{textToAppend}";

        propertyValue.TypedValue.SetValue(MFDataType.MFDatatypeMultiLineText, newText);

        existingProperty!.TypedValue = propertyValue.TypedValue;
    }

    // ── Connection ──

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
