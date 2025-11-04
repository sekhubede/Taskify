using Taskify.Domain.Entities;

namespace Taskify.Domain.Interfaces;

public interface ICommentRepository
{
    /// <summary>
    /// Retrieves all vault comments for a specific assignment.
    /// </summary>
    List<Comment> GetCommentsForAssignment(int assignmentId);

    /// <summary>
    /// Adds a vault comment to an assignment.
    /// Vault comments are visible to all users with access to the assignment.
    /// </summary>
    Comment AddComment(int assignmentId, string content);

    /// <summary>
    /// Gets the count of comments for an assignment.
    /// </summary>
    int GetCommentCount(int assignmentId);
}