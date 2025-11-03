using MFilesAPI;
using Taskify.Domain.Entities;
using Taskify.Domain.Interfaces;

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

            // Get vault comments (visible to all users)
            var vaultComments = GetVaultComments(vault, objID);
            comments.AddRange(vaultComments);

            // Get personal comments (visible only to current user)
            var personalComments = GetPersonalComments(vault, objID);
            comments.AddRange(personalComments);

            // Sort by creation date (newest first)
            return comments.OrderByDescending(c => c.CreatedDate).ToList();
        }
        catch (Exception ex)
        {
            throw new ApplicationException(
                $"Failed to retrieve comments for assignment {assignmentId}", ex);
        }
    }

    public Comment AddPersonalComment(int assignmentId, string content)
    {
        try
        {
            var vault = GetVault();

            var objID = new ObjID();
            objID.SetIDs(
                ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment,
                ID: assignmentId);

            // Get the latest version
            var objVer = vault.ObjectOperations.GetLatestObjVer(objID, AllowCheckedOut: true);

            // Add personal annotation (only visible to current user)
            vault.ObjectOperations.AddAnnotation(
                ObjVer: objVer,
                Annotation: content);

            // Annotations don't have IDs in the same way, so we'll use a timestamp-based pseudo-ID
            var pseudoId = (int)(DateTime.UtcNow.Ticks % int.MaxValue);

            return new Comment(
                id: pseudoId,
                content: content,
                authorName: GetCurrentUserName(vault),
                createdDate: DateTime.UtcNow,
                isPersonal: true,
                assignmentId: assignmentId);
        }
        catch (Exception ex)
        {
            throw new ApplicationException(
                $"Failed to add personal comment to assignment {assignmentId}", ex);
        }
    }

    public Comment AddVaultComment(int assignmentId, string content)
    {
        try
        {
            var vault = GetVault();

            // Create search conditions to find the assignment
            var objID = new ObjID();
            objID.SetIDs(
                ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment,
                ID: assignmentId);

            // Get the object
            var objVer = vault.ObjectOperations.GetLatestObjVer(objID, AllowCheckedOut: true);

            // Check out the object to add a comment
            var checkedOutVersion = vault.ObjectOperations.CheckOut(objID);

            // Get current properties
            var propertyValues = vault.ObjectPropertyOperations.GetProperties(checkedOutVersion.ObjVer);

            // Add or update the comment property
            // M-Files typically uses a multi-line text property for comments
            var commentProperty = propertyValues.SearchForProperty(
                (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefComment);

            string existingComments = string.Empty;
            if (commentProperty != null && !commentProperty.TypedValue.IsNULL())
            {
                existingComments = commentProperty.TypedValue.Value?.ToString() ?? string.Empty;
            }

            // Append new comment with timestamp and author
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            var author = GetCurrentUserName(vault);
            var formattedComment = $"[{timestamp}] {author}: {content}";

            var newCommentText = string.IsNullOrEmpty(existingComments)
                ? formattedComment
                : $"{existingComments}\n\n{formattedComment}";

            var commentPropValue = new PropertyValue
            {
                PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefComment
            };
            commentPropValue.TypedValue.SetValue(MFDataType.MFDatatypeMultiLineText, newCommentText);

            if (commentProperty == null)
            {
                propertyValues.Add(-1, commentPropValue);
            }
            else
            {
                propertyValues.UpdateProperty(commentPropValue);
            }

            // Check in with new comment
            vault.ObjectOperations.CheckIn(checkedOutVersion.ObjVer);
            vault.ObjectPropertyOperations.SetProperties(checkedOutVersion.ObjVer, propertyValues);

            var pseudoId = (int)(DateTime.UtcNow.Ticks % int.MaxValue);

            return new Comment(
                id: pseudoId,
                content: content,
                authorName: author,
                createdDate: DateTime.UtcNow,
                isPersonal: false,
                assignmentId: assignmentId);
        }
        catch (Exception ex)
        {
            throw new ApplicationException(
                $"Failed to add vault comment to assignment {assignmentId}", ex);
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

    private List<Comment> GetVaultComments(MFilesAPI.Vault vault, ObjID objID)
    {
        var comments = new List<Comment>();

        try
        {
            var objVer = vault.ObjectOperations.GetLatestObjVer(objID, AllowCheckedOut: true);
            var propertyValues = vault.ObjectPropertyOperations.GetProperties(objVer);

            var commentProperty = propertyValues.SearchForProperty(
                (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefComment);

            if (commentProperty != null && !commentProperty.TypedValue.IsNULL())
            {
                var commentText = commentProperty.TypedValue.Value?.ToString() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(commentText))
                {
                    // Parse formatted comments (if they follow the pattern)
                    // Format: [timestamp] Author: content
                    var commentLines = commentText.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

                    int commentId = 1;
                    foreach (var line in commentLines)
                    {
                        var parsedComment = ParseCommentLine(line, objID.ID, commentId, isPersonal: false);
                        if (parsedComment != null)
                        {
                            comments.Add(parsedComment);
                            commentId++;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to retrieve vault comments: {ex.Message}");
        }

        return comments;
    }

    private List<Comment> GetPersonalComments(MFilesAPI.Vault vault, ObjID objID)
    {
        var comments = new List<Comment>();

        try
        {
            var objVer = vault.ObjectOperations.GetLatestObjVer(objID, AllowCheckedOut: true);

            // Get annotations (personal notes)
            var annotations = vault.ObjectOperations.GetAnnotations(objVer);

            for (int i = 1; i <= annotations.Count; i++)
            {
                var annotation = annotations[i];

                comments.Add(new Comment(
                    id: i * 10000, // Offset to avoid collision with vault comments
                    content: annotation.Text,
                    authorName: GetCurrentUserName(vault),
                    createdDate: annotation.CreatedUtc,
                    isPersonal: true,
                    assignmentId: objID.ID));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to retrieve personal comments: {ex.Message}");
        }

        return comments;
    }

    private Comment? ParseCommentLine(string line, int assignmentId, int commentId, bool isPersonal)
    {
        try
        {
            // Expected format: [2024-11-03 14:30] John Doe: This is a comment
            var match = System.Text.RegularExpressions.Regex.Match(
                line,
                @"\[([\d\-\s:]+)\]\s+([^:]+):\s+(.+)");

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
                    isPersonal: isPersonal,
                    assignmentId: assignmentId);
            }

            // Fallback: treat entire line as content
            return new Comment(
                id: commentId,
                content: line,
                authorName: "Unknown",
                createdDate: DateTime.UtcNow,
                isPersonal: isPersonal,
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
            var currentUser = vault.UserOperations.GetUserAccount(vault.CurrentLoggedInUserID);
            return currentUser.LoginName;
        }
        catch
        {
            return "Current User";
        }
    }

    private MFilesAPI.Vault GetVault()
    {
        var vaultConnection = _connectionManager.GetVaultConnection();

        if (vaultConnection is not MFilesAPI.Vault vault)
            throw new InvalidOperationException("Failed to get M-Files vault connection");

        return vault;
    }
}