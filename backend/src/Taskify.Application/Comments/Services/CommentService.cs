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

    public Comment AddPersonalComment(int assignmentId, string content)
    {
        ValidateCommentContent(content);

        try
        {
            var comment = _commentRepository.AddPersonalComment(assignmentId, content);
            Console.WriteLine($"Personal comment added to assignment {assignmentId}");
            return comment;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding personal comment: {ex.Message}");
            throw;
        }
    }

    public Comment AddVaultComment(int assignmentId, string content)
    {
        ValidateCommentContent(content);

        try
        {
            var comment = _commentRepository.AddVaultComment(assignmentId, content);
            Console.WriteLine($"Vault comment added to assignment {assignmentId}");
            return comment;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding vault comment: {ex.Message}");
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
            PersonalComments = comments.Count(c => c.IsPersonal),
            VaultComments = comments.Count(c => !c.IsPersonal),
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
    public int PersonalComments { get; set; }
    public int VaultComments { get; set; }
    public int RecentComments { get; set; }
    public DateTime? LatestCommentDate { get; set; }
}