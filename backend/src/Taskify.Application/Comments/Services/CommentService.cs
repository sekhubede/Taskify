using Taskify.Domain.Entities;
using Taskify.Domain.Interfaces;

namespace Taskify.Application.Comments.Services;

public class CommentService
{
    private readonly ICommentRepository _commentRepository;

    public CommentService(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public List<Comment> GetAssignmentComments(int assignmentId)
    {
        try
        {
            return _commentRepository.GetCommentsForAssignment(assignmentId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving comments for assignment {assignmentId}: {ex.Message}");
            throw;
        }
    }

    public Comment AddComment(int assignmentId, string content, string authorName)
    {
        ValidateCommentContent(content);

        try
        {
            var comment = _commentRepository.AddComment(assignmentId, content, authorName);
            Console.WriteLine($"Comment added to assignment {assignmentId}");
            return comment;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding comment: {ex.Message}");
            throw;
        }
    }

    public int GetCommentCount(int assignmentId)
    {
        return _commentRepository.GetCommentCount(assignmentId);
    }

    public CommentSummary GetCommentSummary(int assignmentId)
    {
        var comments = GetAssignmentComments(assignmentId);

        return new CommentSummary
        {
            TotalComments = comments.Count,
            RecentComments = comments.Count(c => c.IsRecentlyAdded()),
            LatestCommentDate = comments.Any() ? comments.Max(c => c.CreatedDate) : (DateTime?)null
        };
    }

    private void ValidateCommentContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content cannot be empty");

        if (content.Length > 5000)
            throw new ArgumentException("Comment content cannot exceed 5000 characters");
    }
}

public class CommentSummary
{
    public int TotalComments { get; set; }
    public int RecentComments { get; set; }
    public DateTime? LatestCommentDate { get; set; }
}
