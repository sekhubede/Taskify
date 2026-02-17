namespace Taskify.Connectors;

/// <summary>
/// The data transfer object that every connector maps TO.
/// This is the application's view of what a "task" looks like.
/// It doesn't care if it came from M-Files, Odoo, or a mock.
/// </summary>
public class TaskDTO
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AssigneeId { get; set; } = string.Empty;
    public string AssigneeName { get; set; } = string.Empty;
    public TaskItemStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>"MFiles", "Odoo", "Mock" - for debugging and auditing.</summary>
    public string SourceSystem { get; set; } = string.Empty;

    /// <summary>The ID in the original system, for deep-linking back.</summary>
    public string SourceId { get; set; } = string.Empty;
}

public enum TaskItemStatus
{
    Open,
    InProgress,
    Blocked,
    Completed,
    Overdue
}
