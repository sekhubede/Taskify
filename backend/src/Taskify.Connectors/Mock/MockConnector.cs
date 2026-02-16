namespace Taskify.Connectors.Mock;

/// <summary>
/// Generates realistic task data for development and testing.
/// No external dependencies. No vault connection. No COM API.
/// Set "DataSource": "Mock" in appsettings.json and build UI features fast.
/// </summary>
public class MockConnector : ITaskDataSource
{
    private readonly List<TaskDTO> _tasks;

    public MockConnector()
    {
        _tasks = GenerateMockTasks();
    }

    public async Task<IReadOnlyList<TaskDTO>> GetAllTasksAsync()
    {
        await Task.Delay(200);
        return _tasks.AsReadOnly();
    }

    public async Task<TaskDTO?> GetTaskByIdAsync(string taskId)
    {
        await Task.Delay(100);
        return _tasks.FirstOrDefault(t => t.Id == taskId);
    }

    public async Task<IReadOnlyList<TaskDTO>> GetTasksByAssigneeAsync(string assigneeId)
    {
        await Task.Delay(200);
        return _tasks.Where(t => t.AssigneeId == assigneeId).ToList().AsReadOnly();
    }

    public Task<bool> UpdateTaskStatusAsync(string taskId, TaskItemStatus newStatus)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId);
        if (task == null) return Task.FromResult(false);

        task.Status = newStatus;
        task.LastUpdatedAt = DateTime.UtcNow;

        if (newStatus == TaskItemStatus.Completed)
            task.CompletedAt = DateTime.UtcNow;

        return Task.FromResult(true);
    }

    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(true);
    }

    public Task<string> GetCurrentUserNameAsync()
    {
        return Task.FromResult("Mock User");
    }

    /// <summary>
    /// Generates a realistic spread of tasks across multiple team members.
    /// Includes edge cases: overdue tasks, blocked tasks, tasks with no due date.
    /// </summary>
    private static List<TaskDTO> GenerateMockTasks()
    {
        var now = DateTime.UtcNow;

        return new List<TaskDTO>
        {
            // Active, on track
            new TaskDTO
            {
                Id = "1001",
                Title = "Implement user authentication flow",
                Description = "Add login, registration, and token refresh logic to the API",
                AssigneeId = "1",
                AssigneeName = "Sarah",
                Status = TaskItemStatus.InProgress,
                DueDate = now.AddDays(2),
                CreatedAt = now.AddDays(-5),
                LastUpdatedAt = now.AddHours(-4),
                SourceSystem = "Mock",
                SourceId = "1001"
            },
            new TaskDTO
            {
                Id = "1002",
                Title = "Write unit tests for payment service",
                Description = "Cover happy path, timeout, and refund scenarios",
                AssigneeId = "1",
                AssigneeName = "Sarah",
                Status = TaskItemStatus.Open,
                DueDate = now.AddDays(5),
                CreatedAt = now.AddDays(-2),
                LastUpdatedAt = now.AddDays(-1),
                SourceSystem = "Mock",
                SourceId = "1002"
            },

            // Has a blocker
            new TaskDTO
            {
                Id = "1003",
                Title = "Integrate third-party shipping API",
                Description = "Connect to FedEx API, handle rate calculation and label generation",
                AssigneeId = "2",
                AssigneeName = "James",
                Status = TaskItemStatus.Blocked,
                DueDate = now.AddDays(1),
                CreatedAt = now.AddDays(-7),
                LastUpdatedAt = now.AddDays(-2),
                SourceSystem = "Mock",
                SourceId = "1003"
            },
            new TaskDTO
            {
                Id = "1004",
                Title = "Refactor database connection pooling",
                Description = "Replace singleton pattern with proper connection pool management",
                AssigneeId = "2",
                AssigneeName = "James",
                Status = TaskItemStatus.InProgress,
                DueDate = now.AddDays(4),
                CreatedAt = now.AddDays(-3),
                LastUpdatedAt = now.AddHours(-6),
                SourceSystem = "Mock",
                SourceId = "1004"
            },

            // Overdue task
            new TaskDTO
            {
                Id = "1005",
                Title = "Fix CSV export formatting bug",
                Description = "Special characters in field values break the CSV output",
                AssigneeId = "3",
                AssigneeName = "Thabo",
                Status = TaskItemStatus.Open,
                DueDate = now.AddDays(-3),
                CreatedAt = now.AddDays(-10),
                LastUpdatedAt = now.AddDays(-4),
                SourceSystem = "Mock",
                SourceId = "1005"
            },
            new TaskDTO
            {
                Id = "1006",
                Title = "Add pagination to the reports endpoint",
                Description = "Reports endpoint returns all records - needs limit/offset support",
                AssigneeId = "3",
                AssigneeName = "Thabo",
                Status = TaskItemStatus.InProgress,
                DueDate = now.AddDays(3),
                CreatedAt = now.AddDays(-4),
                LastUpdatedAt = now.AddHours(-2),
                SourceSystem = "Mock",
                SourceId = "1006"
            },

            // Completed task (for dashboard variety)
            new TaskDTO
            {
                Id = "1007",
                Title = "Set up CI/CD pipeline for staging environment",
                Description = "GitHub Actions workflow for automated deployment to staging",
                AssigneeId = "4",
                AssigneeName = "Lebo",
                Status = TaskItemStatus.Completed,
                DueDate = now.AddDays(-1),
                CreatedAt = now.AddDays(-8),
                LastUpdatedAt = now.AddHours(-1),
                CompletedAt = now.AddHours(-1),
                SourceSystem = "Mock",
                SourceId = "1007"
            },
            new TaskDTO
            {
                Id = "1008",
                Title = "Investigate memory leak in worker service",
                Description = "Worker service memory grows 15% per hour under load",
                AssigneeId = "4",
                AssigneeName = "Lebo",
                Status = TaskItemStatus.Open,
                DueDate = now.AddDays(2),
                CreatedAt = now.AddDays(-1),
                LastUpdatedAt = now.AddHours(-3),
                SourceSystem = "Mock",
                SourceId = "1008"
            },

            // Edge case: no due date
            new TaskDTO
            {
                Id = "1009",
                Title = "Update internal documentation wiki",
                Description = "API docs and onboarding guide are out of date",
                AssigneeId = "1",
                AssigneeName = "Sarah",
                Status = TaskItemStatus.Open,
                DueDate = null,
                CreatedAt = now.AddDays(-15),
                LastUpdatedAt = now.AddDays(-10),
                SourceSystem = "Mock",
                SourceId = "1009"
            }
        };
    }
}
