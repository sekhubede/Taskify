using Taskify.Domain.Entities;

namespace Taskify.Domain.Interfaces;

public interface ICommentRepository
{
    /// <summary>
    /// Retrieves all comments for a specific assignment.
    /// </summary>
    List<Comment> GetCommentsForAssignment(int assignmentId);

    /// <summary>
    /// Adds a comment to an assignment.
    /// </summary>
    Comment AddComment(int assignmentId, string content, string authorName);

    /// <summary>
    /// Gets the count of comments for an assignment.
    /// </summary>
    int GetCommentCount(int assignmentId);
}
