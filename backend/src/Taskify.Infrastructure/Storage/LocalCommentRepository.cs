using Taskify.Domain.Entities;
using Taskify.Domain.Interfaces;

namespace Taskify.Infrastructure.Storage;

public class LocalCommentRepository : ICommentRepository
{
    private readonly LocalCommentStore _store;

    public LocalCommentRepository(LocalCommentStore store)
    {
        _store = store;
    }

    public List<Comment> GetCommentsForAssignment(int assignmentId)
    {
        var items = _store.GetCommentsForAssignment(assignmentId);

        return items.Select(item => new Comment(
            id: item.Id,
            content: item.Content,
            authorName: item.AuthorName,
            createdDate: item.CreatedDate,
            assignmentId: assignmentId
        )).ToList();
    }

    public Comment AddComment(int assignmentId, string content, string authorName)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentNullException(nameof(content));

        if (assignmentId <= 0)
            throw new ArgumentException("Assignment ID must be positive", nameof(assignmentId));

        var item = _store.AddComment(assignmentId, content.Trim(), authorName);

        return new Comment(
            id: item.Id,
            content: item.Content,
            authorName: item.AuthorName,
            createdDate: item.CreatedDate,
            assignmentId: assignmentId
        );
    }

    public int GetCommentCount(int assignmentId)
    {
        return _store.GetCommentCount(assignmentId);
    }
}
