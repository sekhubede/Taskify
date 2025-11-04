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

            var objVer = vault.ObjectOperations.GetLatestObjVer(objID, AllowCheckedOut: true);
            var propertyValues = vault.ObjectPropertyOperations.GetProperties(objVer);

            if (propertyValues.IndexOf((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefComment) == -1)
                return new List<Comment>();

            var commentProperty = propertyValues.SearchForProperty(
                (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefComment);

            if (commentProperty != null && !commentProperty.TypedValue.IsNULL())
            {
                var commentText = commentProperty.TypedValue.Value?.ToString() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(commentText))
                {
                    // Parse formatted comments
                    // Format: [timestamp] Author: content
                    var commentLines = commentText.Split(
                        new[] { "\n\n" },
                        StringSplitOptions.RemoveEmptyEntries);

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

            // Sort by creation date (newest first)
            return comments.OrderByDescending(c => c.CreatedDate).ToList();
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
        try
        {
            // Attempt to get from session info (may not always work with client app)
            var sessionInfo = vault.SessionInfo;
            if (sessionInfo != null)
            {
                return sessionInfo.AccountName ?? "Current User";
            }
        }
        catch { }

        // Ultimate fallback: just use a generic identifier
        return $"User_{vault.CurrentLoggedInUserID}";
    }

    private MFilesAPI.Vault GetVault()
    {
        var vaultConnection = _connectionManager.GetVaultConnection();

        if (vaultConnection is not MFilesAPI.Vault vault)
            throw new InvalidOperationException("Failed to get M-Files vault connection");

        return vault;
    }
}