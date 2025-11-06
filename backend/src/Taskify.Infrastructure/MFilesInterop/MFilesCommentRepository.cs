using MFilesAPI;
using Taskify.Domain.Entities;
using Taskify.Domain.Interfaces;
using Taskify.Infrastructure.Helpers;

namespace Taskify.Infrastructure.MFilesInterop;

public class MFilesCommentRepository : ICommentRepository
{
    private readonly IVaultConnectionManager _connectionManager;

    public MFilesCommentRepository(IVaultConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public List<Comment> GetCommentsForAssignment(int assignmentId)
    {
        try
        {
            var vault = GetVault();
            var comments = new List<Comment>();

            var objID = new ObjID();
            objID.SetIDs(
                ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment,
                ID: assignmentId);

            // Prefer version-specific comments by iterating versions from latest to first
            try
            {
                var latest = vault.ObjectOperations.GetLatestObjVer(objID, AllowCheckedOut: true);
                for (int v = latest.Version; v >= 1; v--)
                {
                    var objVer = new ObjVer();
                    objVer.SetIDs((int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment, assignmentId, v);

                    VersionComment versionComment = null;
                    try
                    {
                        var vc = vault.ObjectPropertyOperations.GetVersionComment(objVer);
                        versionComment = vc ?? null;
                    }
                    catch { }

                    if (null != versionComment)
                    {
                        comments.Add(new Comment(
                            id: versionComment.ObjVer.Version,
                            content: versionComment.VersionComment.Value.DisplayValue,
                            authorName: versionComment.LastModifiedBy.Value.DisplayValue,
                            createdDate: (DateTime)versionComment.StatusChanged.Value.GetValueAsTimestamp().UtcToLocalTime().GetValue(),
                            assignmentId: assignmentId));
                    }
                }
            }
            catch { }

            // Fallback to multi-line comment property on latest version if no version comments found
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
                                var parsedComment = ParseCommentLine(line, assignmentId, commentId);
                                if (parsedComment != null)
                                {
                                    comments.Add(parsedComment);
                                    commentId++;
                                }
                            }
                        }
                    }
                }
            }

            // Sort by ID ascending (newest comments with higher IDs at bottom)
            return comments.OrderBy(c => c.Id).ToList();
        }
        catch (Exception ex)
        {
            throw new ApplicationException(
                $"Failed to retrieve comments for assignment {assignmentId}", ex);
        }
    }

    public Comment AddComment(int assignmentId, string content)
    {
        var vault = GetVault();

        var objID = new ObjID();
        objID.SetIDs(
            ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment,
            ID: assignmentId);

        try
        {
            // Check out the object
            var checkedOutVersion = vault.ObjectOperations.CheckOut(objID);

            // Get current properties
            var propertyValues = vault.ObjectPropertyOperations.GetProperties(checkedOutVersion.ObjVer);

            // Format the new comment
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            var author = GetCurrentUserName(vault);
            var formattedComment = $"[{timestamp}] {author}: {content}";

            // Append to existing comments
            PropertyValueHelper.AppendToMultiLineTextProperty(
                propertyValues,
                (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefComment,
                formattedComment);

            // Check in with updated properties
            vault.ObjectPropertyOperations.SetAllProperties(checkedOutVersion.ObjVer, AllowModifyingCheckedInObject: true, propertyValues);
            vault.ObjectOperations.CheckIn(checkedOutVersion.ObjVer);

            var pseudoId = (int)(DateTime.UtcNow.Ticks % int.MaxValue);

            return new Comment(
                id: pseudoId,
                content: content,
                authorName: author,
                createdDate: DateTime.UtcNow,
                assignmentId: assignmentId);
        }
        catch (Exception ex)
        {
            throw new ApplicationException(
                $"Failed to add comment to assignment {assignmentId}", ex);
        }
    }

    public int GetCommentCount(int assignmentId)
    {
        try
        {
            var comments = GetCommentsForAssignment(assignmentId);
            return comments.Count;
        }
        catch
        {
            return 0;
        }
    }

    private Comment? ParseCommentLine(string line, int assignmentId, int commentId)
    {
        try
        {
            // Expected format: [2024-11-03 14:30] John Doe: This is a comment
            var match = System.Text.RegularExpressions.Regex.Match(
                line,
                @"\[([\d\-\s:]+)\]\s+([^:]+):\s+(.+)",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            if (match.Success)
            {
                var timestamp = DateTime.Parse(match.Groups[1].Value);
                var author = match.Groups[2].Value.Trim();
                var content = match.Groups[3].Value.Trim();

                return new Comment(
                    id: commentId,
                    content: content,
                    authorName: author,
                    createdDate: timestamp,
                    assignmentId: assignmentId);
            }

            // Fallback: treat entire line as content
            return new Comment(
                id: commentId,
                content: line,
                authorName: "Unknown",
                createdDate: DateTime.UtcNow,
                assignmentId: assignmentId);
        }
        catch
        {
            return null;
        }
    }

    private string GetCurrentUserName(MFilesAPI.Vault vault)
    {
        // When adding comments via multi-line property, we need to match the format
        // used when retrieving comments from version comments (LastModifiedBy.DisplayValue)
        // The simplest approach is to create a temporary object version comment to get the DisplayValue
        // But that's complex. Instead, we'll use a lookup value approach.

        try
        {
            // Create a lookup value for the current user to get DisplayValue
            // This matches how versionComment.LastModifiedBy.Value.DisplayValue works
            var userLookupValue = new TypedValue();
            userLookupValue.SetValue(MFDataType.MFDatatypeLookup, vault.CurrentLoggedInUserID);

            // Get the display value from the lookup
            var displayValue = userLookupValue.DisplayValue;
            if (!string.IsNullOrWhiteSpace(displayValue))
                return displayValue;
        }
        catch { }

        // Fallback: try to get from session info
        try
        {
            var sessionInfo = vault.SessionInfo;
            if (sessionInfo != null && !string.IsNullOrWhiteSpace(sessionInfo.AccountName))
            {
                return sessionInfo.AccountName;
            }
        }
        catch { }

        // Ultimate fallback: just use a generic identifier
        return $"User_{vault.CurrentLoggedInUserID}";
    }

    private static T? GetPropertyValue<T>(PropertyValues properties, int propertyDef)
    {
        try
        {
            var propValue = properties.SearchForProperty(propertyDef);
            if (propValue == null || propValue.TypedValue.IsNULL() || propValue.TypedValue.IsUninitialized())
                return default;

            var value = propValue.TypedValue.Value;

            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                if (value is DateTime dt)
                    return (T)(object)dt;
                return default;
            }

            return (T)value;
        }
        catch
        {
            return default;
        }
    }

    private MFilesAPI.Vault GetVault()
    {
        var vaultConnection = _connectionManager.GetVaultConnection();

        if (vaultConnection is not MFilesAPI.Vault vault)
            throw new InvalidOperationException("Failed to get M-Files vault connection");

        return vault;
    }

    private static T? SafeGet<T>(Func<T> getter)
    {
        try { return getter(); } catch { return default; }
    }
}