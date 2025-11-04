namespace Taskify.Domain.Entities;

public class Comment
{
    public int Id { get; private set; }
    public string Content { get; private set; }
    public string AuthorName { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public int AssignmentId { get; private set; }

    private const int MaxContentLength = 5000;

    public Comment(
        int id,
        string content,
        string authorName,
        DateTime createdDate,
        int assignmentId)
    {
        if (id <= 0)
            throw new ArgumentException("Comment ID must be positive", nameof(id));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentNullException(nameof(content), "Comment content cannot be empty");

        if (content.Length > MaxContentLength)
            throw new ArgumentException(
                $"Comment content cannot exceed {MaxContentLength} characters",
                nameof(content));

        if (string.IsNullOrWhiteSpace(authorName))
            throw new ArgumentNullException(nameof(authorName), "Comment must have an author");

        if (assignmentId <= 0)
            throw new ArgumentException("Assignment ID must be positive", nameof(assignmentId));

        Id = id;
        Content = content;
        AuthorName = authorName;
        CreatedDate = createdDate;
        AssignmentId = assignmentId;
    }

    public bool IsRecentlyAdded(int hoursThreshold = 24)
    {
        return (DateTime.UtcNow - CreatedDate).TotalHours <= hoursThreshold;
    }

    public string GetContentPreview(int maxLength = 100)
    {
        if (Content.Length <= maxLength)
            return Content;

        return Content.Substring(0, maxLength) + "...";
    }
}