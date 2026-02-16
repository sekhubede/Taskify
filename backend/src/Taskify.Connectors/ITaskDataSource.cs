namespace Taskify.Connectors;

/// <summary>
/// The contract every external data source must implement.
/// The application never talks directly to M-Files, Odoo, or any other system.
/// It talks to this interface. The concrete implementation is swapped via config.
/// </summary>
public interface ITaskDataSource
{
    /// <summary>
    /// Pulls all active tasks/assignments from the source system.
    /// </summary>
    Task<IReadOnlyList<TaskDTO>> GetAllTasksAsync();

    /// <summary>
    /// Gets a single task with full details from the source system.
    /// </summary>
    Task<TaskDTO?> GetTaskByIdAsync(string taskId);

    /// <summary>
    /// Gets all tasks assigned to a specific user.
    /// </summary>
    Task<IReadOnlyList<TaskDTO>> GetTasksByAssigneeAsync(string assigneeId);

    /// <summary>
    /// Updates the status of a task in the source system.
    /// Not all source systems will support this - implementations can throw
    /// NotSupportedException if the source is read-only.
    /// </summary>
    Task<bool> UpdateTaskStatusAsync(string taskId, TaskItemStatus newStatus);

    /// <summary>
    /// Health check - can we reach the source system right now?
    /// Useful for the dashboard to show connection status.
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Gets the display name of the currently authenticated user.
    /// Returns the source system's concept of "current user".
    /// </summary>
    Task<string> GetCurrentUserNameAsync();
}
