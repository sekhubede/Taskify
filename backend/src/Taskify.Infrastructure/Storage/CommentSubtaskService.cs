namespace Taskify.Infrastructure.Storage;

public class CommentSubtaskService
{
    private readonly CommentSubtaskStore _store;

    public CommentSubtaskService(CommentSubtaskStore store)
    {
        _store = store;
    }

    public List<CommentSubtaskItem> GetCommentSubtasks(int commentId)
    {
        if (commentId <= 0)
            throw new ArgumentException("Comment ID must be positive", nameof(commentId));

        return _store.GetSubtasksForComment(commentId);
    }

    public CommentSubtaskItem AddCommentSubtask(int commentId, string title, int? order = null)
    {
        if (commentId <= 0)
            throw new ArgumentException("Comment ID must be positive", nameof(commentId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Subtask title cannot be empty", nameof(title));

        var trimmed = title.Trim();
        if (trimmed.Length > 200)
            throw new ArgumentException("Subtask title cannot exceed 200 characters", nameof(title));

        return _store.AddSubtask(commentId, trimmed, order);
    }

    public bool ToggleCommentSubtaskCompletion(int subtaskId, bool isCompleted)
    {
        if (subtaskId <= 0)
            throw new ArgumentException("Subtask ID must be positive", nameof(subtaskId));

        return _store.SetCompletion(subtaskId, isCompleted);
    }

    public bool UpdateCommentSubtaskTitle(int subtaskId, string title)
    {
        if (subtaskId <= 0)
            throw new ArgumentException("Subtask ID must be positive", nameof(subtaskId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Subtask title cannot be empty", nameof(title));

        var trimmed = title.Trim();
        if (trimmed.Length > 200)
            throw new ArgumentException("Subtask title cannot exceed 200 characters", nameof(title));

        return _store.SetTitle(subtaskId, trimmed);
    }

    public bool DeleteCommentSubtask(int subtaskId)
    {
        if (subtaskId <= 0)
            throw new ArgumentException("Subtask ID must be positive", nameof(subtaskId));

        return _store.DeleteSubtask(subtaskId);
    }

    public void ReorderCommentSubtasks(int commentId, Dictionary<int, int> subtaskIdToOrder)
    {
        if (commentId <= 0)
            throw new ArgumentException("Comment ID must be positive", nameof(commentId));
        if (subtaskIdToOrder == null || subtaskIdToOrder.Count == 0)
            throw new ArgumentException("Subtask order mapping cannot be empty", nameof(subtaskIdToOrder));

        _store.ReorderSubtasks(commentId, subtaskIdToOrder);
    }
}
