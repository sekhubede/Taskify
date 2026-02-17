using Taskify.Connectors;
using Taskify.Domain.Entities;

namespace Taskify.Application.Comments.Services;

public class CommentService
{
    private readonly ITaskDataSource _dataSource;

    public CommentService(ITaskDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public List<Comment> GetAssignmentComments(int assignmentId)
    {
        try
        {
            var dtos = _dataSource.GetCommentsForTaskAsync(assignmentId.ToString())
                .GetAwaiter().GetResult();

            return dtos.Select(dto => new Comment(
                id: dto.Id,
                content: dto.Content,
                authorName: dto.AuthorName,
                createdDate: dto.CreatedDate,
                assignmentId: assignmentId
            )).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving comments for assignment {assignmentId}: {ex.Message}");
            throw;
        }
    }

    public Comment AddComment(int assignmentId, string content)
    {
        ValidateCommentContent(content);

        try
        {
            var dto = _dataSource.AddCommentAsync(assignmentId.ToString(), content)
                .GetAwaiter().GetResult();

            Console.WriteLine($"Comment added to assignment {assignmentId}");

            return new Comment(
                id: dto.Id,
                content: dto.Content,
                authorName: dto.AuthorName,
                createdDate: dto.CreatedDate,
                assignmentId: assignmentId
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding comment: {ex.Message}");
            throw;
        }
    }

    public int GetCommentCount(int assignmentId)
    {
        return _dataSource.GetCommentCountAsync(assignmentId.ToString())
            .GetAwaiter().GetResult();
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
