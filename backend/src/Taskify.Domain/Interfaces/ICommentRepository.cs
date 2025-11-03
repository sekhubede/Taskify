using Taskify.Domain.Entities;

namespace Taskify.Domain.Interfaces;

public interface ICommentRepository
{
    /// <summary>
    /// Retrieves all comments for a specific assignment.
    /// Includes both personal and vault comments.
    /// </summary>
    List<Comment> GetCommentsForAssignment(int assignmentId);

    /// <summary>
    /// Adds a personal comment to an assignment.
    /// Personal comments are only visible to the current user.
    /// </summary>
    Comment AddPersonalComment(int assignmentId, string content);

    /// <summary>
    /// Adds a vault comment to an assignment.
    /// Vault comments are visible to all users with access to the assignment.
    /// </summary>
    Comment AddVaultComment(int assignmentId, string content);

    /// <summary>
    /// Gets the count of comments for an assignment.
    /// </summary>
    int GetCommentCount(int assignmentId);
}