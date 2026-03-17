namespace Taskify.Infrastructure.Storage;

public class QuickTaskService
{
    private readonly QuickTaskStore _store;

    public QuickTaskService(QuickTaskStore store)
    {
        _store = store;
    }

    public List<QuickTaskItem> GetTasks() => _store.GetTasks();

    public QuickTaskItem AddTask(string title)
    {
        var trimmed = ValidateTitle(title, "Task title");
        return _store.AddTask(trimmed);
    }

    public bool UpdateTaskTitle(int taskId, string title)
    {
        EnsurePositive(taskId, "Task ID");
        var trimmed = ValidateTitle(title, "Task title");
        return _store.UpdateTaskTitle(taskId, trimmed);
    }

    public bool ToggleTaskCompletion(int taskId, bool isCompleted)
    {
        EnsurePositive(taskId, "Task ID");
        return _store.SetTaskCompletion(taskId, isCompleted);
    }

    public bool DeleteTask(int taskId)
    {
        EnsurePositive(taskId, "Task ID");
        return _store.DeleteTask(taskId);
    }

    public List<QuickTaskCommentItem> GetTaskComments(int taskId)
    {
        EnsurePositive(taskId, "Task ID");
        return _store.GetTaskComments(taskId);
    }

    public QuickTaskCommentItem AddTaskComment(int taskId, string content)
    {
        EnsurePositive(taskId, "Task ID");
        var trimmed = ValidateComment(content);
        return _store.AddTaskComment(taskId, trimmed);
    }

    public bool UpdateTaskComment(int commentId, string content)
    {
        EnsurePositive(commentId, "Comment ID");
        var trimmed = ValidateComment(content);
        return _store.SetTaskCommentContent(commentId, trimmed);
    }

    public bool DeleteTaskComment(int commentId)
    {
        EnsurePositive(commentId, "Comment ID");
        return _store.DeleteTaskComment(commentId);
    }

    public List<QuickTaskChecklistItem> GetTaskChecklist(int taskId)
    {
        EnsurePositive(taskId, "Task ID");
        return _store.GetTaskChecklist(taskId);
    }

    public QuickTaskChecklistItem AddChecklistItem(int taskId, string title, int? order = null)
    {
        EnsurePositive(taskId, "Task ID");
        var trimmed = ValidateTitle(title, "Checklist title");
        return _store.AddChecklistItem(taskId, trimmed, order);
    }

    public bool ToggleChecklistItem(int checklistItemId, bool isCompleted)
    {
        EnsurePositive(checklistItemId, "Checklist item ID");
        return _store.SetChecklistCompletion(checklistItemId, isCompleted);
    }

    public bool UpdateChecklistItemTitle(int checklistItemId, string title)
    {
        EnsurePositive(checklistItemId, "Checklist item ID");
        var trimmed = ValidateTitle(title, "Checklist title");
        return _store.SetChecklistTitle(checklistItemId, trimmed);
    }

    public bool DeleteChecklistItem(int checklistItemId)
    {
        EnsurePositive(checklistItemId, "Checklist item ID");
        return _store.DeleteChecklistItem(checklistItemId);
    }

    public void ReorderChecklist(int taskId, Dictionary<int, int> checklistOrders)
    {
        EnsurePositive(taskId, "Task ID");
        if (checklistOrders == null || checklistOrders.Count == 0)
            throw new ArgumentException("Checklist order mapping cannot be empty", nameof(checklistOrders));

        _store.ReorderChecklist(taskId, checklistOrders);
    }

    private static string ValidateTitle(string? title, string label)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException($"{label} cannot be empty");

        var trimmed = title.Trim();
        if (trimmed.Length > 200)
            throw new ArgumentException($"{label} cannot exceed 200 characters");

        return trimmed;
    }

    private static string ValidateComment(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content cannot be empty");

        var trimmed = content.Trim();
        if (trimmed.Length > 5000)
            throw new ArgumentException("Comment content cannot exceed 5000 characters");

        return trimmed;
    }

    private static void EnsurePositive(int id, string label)
    {
        if (id <= 0)
            throw new ArgumentException($"{label} must be positive");
    }
}
