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

        // Active-only list by default to avoid pulling large historical completed datasets.
        var completedCondition = new SearchCondition
        {
            ConditionType = MFConditionType.MFConditionTypeEqual
        };
        completedCondition.Expression.SetPropertyValueExpression(
            (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefCompleted,
            MFParentChildBehavior.MFParentChildBehaviorNone);
        completedCondition.TypedValue.SetValue(MFDataType.MFDatatypeBoolean, false);
        searchConditions.Add(-1, completedCondition);

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
        var vault = GetVault();
        int userId;

        if (!int.TryParse(assigneeId, out userId))
        {
            var currentName = GetCurrentUserNameAsync().GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(currentName) ||
                !string.Equals(currentName.Trim(), assigneeId?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<IReadOnlyList<TaskDTO>>(new List<TaskDTO>());
            }

            userId = vault.CurrentLoggedInUserID;
        }

        var searchConditions = new SearchConditions();

        var deletedCondition = new SearchCondition
        {
            ConditionType = MFConditionType.MFConditionTypeEqual
        };
        deletedCondition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeDeleted);
        deletedCondition.TypedValue.SetValue(MFDataType.MFDatatypeBoolean, false);
        searchConditions.Add(-1, deletedCondition);

        var objectTypeCondition = new SearchCondition
        {
            ConditionType = MFConditionType.MFConditionTypeEqual
        };
        objectTypeCondition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectTypeID);
        objectTypeCondition.TypedValue.SetValue(
            MFDataType.MFDatatypeLookup,
            (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment);
        searchConditions.Add(-1, objectTypeCondition);

        var completedCondition = new SearchCondition
        {
            ConditionType = MFConditionType.MFConditionTypeEqual
        };
        completedCondition.Expression.SetPropertyValueExpression(
            (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefCompleted,
            MFParentChildBehavior.MFParentChildBehaviorNone);
        completedCondition.TypedValue.SetValue(MFDataType.MFDatatypeBoolean, false);
        searchConditions.Add(-1, completedCondition);

        var assignedToCondition = new SearchCondition
        {
            ConditionType = MFConditionType.MFConditionTypeEqual
        };
        assignedToCondition.Expression.SetPropertyValueExpression(
            (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefAssignedTo,
            MFParentChildBehavior.MFParentChildBehaviorNone);
        assignedToCondition.TypedValue.SetValue(
            MFDataType.MFDatatypeLookup,
            userId);
        searchConditions.Add(-1, assignedToCondition);

        var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditions(
            searchConditions,
            MFSearchFlags.MFSearchFlagNone,
            SortResults: false);

        var tasks = MFilesTaskMapper.MapAll(vault, searchResults);
        return Task.FromResult<IReadOnlyList<TaskDTO>>(tasks);
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

    public Task<IReadOnlyList<AttachmentDTO>> GetAttachmentsForTaskAsync(string taskId)
    {
        if (!int.TryParse(taskId, out var assignmentId))
            return Task.FromResult<IReadOnlyList<AttachmentDTO>>(new List<AttachmentDTO>());

        try
        {
            var vault = GetVault();

            var objID = new ObjID();
            objID.SetIDs(
                ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment,
                ID: assignmentId);

            var latestObjVer = vault.ObjectOperations.GetLatestObjVer(objID, AllowCheckedOut: true);
            var files = vault.ObjectFileOperations.GetFiles(latestObjVer);

            var attachments = new List<AttachmentDTO>();
            for (int i = 1; i <= files.Count; i++)
            {
                var file = files[i];
                var fileName = string.IsNullOrWhiteSpace(file.Extension)
                    ? file.Title
                    : $"{file.Title}.{file.Extension}";

                long sizeBytes = 0;
                try
                {
                    sizeBytes = vault.ObjectFileOperations.GetFileSizeEx(objID, file.FileVer);
                }
                catch
                {
                    try { sizeBytes = vault.ObjectFileOperations.GetFileSize(file.FileVer); } catch { }
                }

                attachments.Add(new AttachmentDTO
                {
                    Id = file.ID.ToString(),
                    FileName = fileName,
                    ContentType = GetContentTypeFromFileName(fileName),
                    SizeBytes = sizeBytes
                });
            }

            return Task.FromResult<IReadOnlyList<AttachmentDTO>>(attachments);
        }
        catch (Exception ex)
        {
            throw new ApplicationException(
                $"Failed to retrieve attachments for assignment {assignmentId}: {ex.Message}", ex);
        }
    }

    public Task<AttachmentDTO> AddAttachmentAsync(
        string taskId,
        string fileName,
        string contentType,
        byte[] content)
    {
        if (!int.TryParse(taskId, out var assignmentId))
            throw new ArgumentException("Invalid task ID", nameof(taskId));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required", nameof(fileName));

        var extension = Path.GetExtension(fileName).TrimStart('.');
        if (string.IsNullOrWhiteSpace(extension))
            extension = "bin";

        var title = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(title))
            title = fileName;

        var tempPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}_{SanitizeFileName(fileName)}");

        try
        {
            File.WriteAllBytes(tempPath, content ?? Array.Empty<byte>());

            var vault = GetVault();

            var objID = new ObjID();
            objID.SetIDs(
                ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment,
                ID: assignmentId);

            var checkedOutVersion = vault.ObjectOperations.CheckOut(objID);
            try
            {
                var addedFileVer = vault.ObjectFileOperations.AddFile(
                    checkedOutVersion.ObjVer,
                    title,
                    extension,
                    tempPath);

                vault.ObjectOperations.CheckIn(checkedOutVersion.ObjVer);

                var addedFileName = $"{title}.{extension}";
                var result = new AttachmentDTO
                {
                    Id = addedFileVer.ID.ToString(),
                    FileName = addedFileName,
                    ContentType = string.IsNullOrWhiteSpace(contentType)
                        ? GetContentTypeFromFileName(addedFileName)
                        : contentType,
                    SizeBytes = content?.LongLength ?? 0
                };

                return Task.FromResult(result);
            }
            catch
            {
                try { vault.ObjectOperations.UndoCheckout(checkedOutVersion.ObjVer); } catch { }
                throw;
            }
        }
        catch (Exception ex)
        {
            throw new ApplicationException(
                $"Failed to add attachment to assignment {assignmentId}: {ex.Message}", ex);
        }
        finally
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
        }
    }

    public Task<AttachmentFileDTO?> GetAttachmentFileAsync(string taskId, string attachmentId)
    {
        if (!int.TryParse(taskId, out var assignmentId))
            return Task.FromResult<AttachmentFileDTO?>(null);
        if (!int.TryParse(attachmentId, out var fileId))
            return Task.FromResult<AttachmentFileDTO?>(null);

        try
        {
            var vault = GetVault();

            var objID = new ObjID();
            objID.SetIDs(
                ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment,
                ID: assignmentId);

            var latestObjVer = vault.ObjectOperations.GetLatestObjVer(objID, AllowCheckedOut: true);
            var files = vault.ObjectFileOperations.GetFiles(latestObjVer);

            ObjectFile? targetFile = null;
            for (int i = 1; i <= files.Count; i++)
            {
                var file = files[i];
                if (file.ID == fileId)
                {
                    targetFile = file;
                    break;
                }
            }

            if (targetFile == null)
                return Task.FromResult<AttachmentFileDTO?>(null);

            var dataUri = vault.ObjectFileOperations.DownloadFileAsDataURIEx(
                latestObjVer,
                targetFile.FileVer);
            if (string.IsNullOrWhiteSpace(dataUri))
                return Task.FromResult<AttachmentFileDTO?>(null);

            var commaIndex = dataUri.IndexOf(',');
            if (commaIndex <= 0)
                throw new ApplicationException("M-Files returned an invalid data URI payload.");

            var metadata = dataUri.Substring(0, commaIndex);
            var payload = dataUri[(commaIndex + 1)..];
            var isBase64 = metadata.Contains(";base64", StringComparison.OrdinalIgnoreCase);

            var fileName = string.IsNullOrWhiteSpace(targetFile.Extension)
                ? targetFile.Title
                : $"{targetFile.Title}.{targetFile.Extension}";

            var mimeType = GetContentTypeFromFileName(fileName);
            if (metadata.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var contentTypePart = metadata["data:".Length..].Split(';')[0];
                if (!string.IsNullOrWhiteSpace(contentTypePart))
                    mimeType = contentTypePart;
            }

            var bytes = isBase64
                ? Convert.FromBase64String(payload)
                : System.Text.Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));

            var dto = new AttachmentFileDTO
            {
                Id = targetFile.ID.ToString(),
                FileName = fileName,
                ContentType = mimeType,
                Content = bytes
            };

            return Task.FromResult<AttachmentFileDTO?>(dto);
        }
        catch (Exception ex)
        {
            throw new ApplicationException(
                $"Failed to download attachment {attachmentId} for assignment {assignmentId}: {ex.Message}", ex);
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

    private static string GetContentTypeFromFileName(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "file.bin";

        var sanitized = fileName;
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(sanitized) ? "file.bin" : sanitized;
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
