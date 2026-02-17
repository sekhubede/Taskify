using Taskify.Connectors;
using Taskify.Domain.Entities;

namespace Taskify.Application.Assignments.Services;

public class AssignmentService
{
    private readonly ITaskDataSource _dataSource;
    private readonly ISubtaskLoader _subtaskLoader;

    public AssignmentService(ITaskDataSource dataSource, ISubtaskLoader subtaskLoader)
    {
        _dataSource = dataSource;
        _subtaskLoader = subtaskLoader;
    }

    public List<Assignment> GetUserAssignments()
    {
        try
        {
            var tasks = _dataSource.GetAllTasksAsync().GetAwaiter().GetResult();
            var assignments = tasks.Select(t => MapToAssignment(t)).ToList();

            return assignments
                .OrderByDescending(a => a.IsOverdue())
                .ThenBy(a => a.IsOverdue()
                    ? -(a.DueDate?.Ticks ?? long.MinValue)
                    : (a.DueDate?.Ticks ?? long.MaxValue))
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving assignments: {ex.Message}");
            throw;
        }
    }

    public Assignment? GetAssignment(int assignmentId)
    {
        try
        {
            var task = _dataSource.GetTaskByIdAsync(assignmentId.ToString()).GetAwaiter().GetResult();
            return task != null ? MapToAssignment(task) : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving assignment {assignmentId}: {ex.Message}");
            return null;
        }
    }

    public bool CompleteAssignment(int assignmentId)
    {
        try
        {
            return _dataSource.UpdateTaskStatusAsync(assignmentId.ToString(), TaskItemStatus.Completed)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error completing assignment {assignmentId}: {ex.Message}");
            return false;
        }
    }

    public AssignmentSummary GetAssignmentSummary()
    {
        var assignments = GetUserAssignments();

        return new AssignmentSummary
        {
            TotalAssignments = assignments.Count,
            CompletedAssignments = assignments.Count(a => a.Status == AssignmentStatus.Completed),
            OverdueAssignments = assignments.Count(a => a.IsOverdue()),
            DueSoonAssignments = assignments.Count(a => a.IsDueSoon())
        };
    }

    private Assignment MapToAssignment(TaskDTO task)
    {
        var id = int.TryParse(task.Id, out var parsedId) ? parsedId : task.Id.GetHashCode();

        var subtasks = _subtaskLoader.LoadSubtasks(id);

        return new Assignment(
            id: id,
            title: task.Title,
            description: task.Description,
            dueDate: task.DueDate,
            status: MapStatus(task.Status),
            assignedTo: task.AssigneeName,
            createdDate: task.CreatedAt,
            completedDate: task.CompletedAt,
            subtasks: subtasks
        );
    }

    private static AssignmentStatus MapStatus(TaskItemStatus status)
    {
        return status switch
        {
            TaskItemStatus.Open => AssignmentStatus.NotStarted,
            TaskItemStatus.InProgress => AssignmentStatus.InProgress,
            TaskItemStatus.Blocked => AssignmentStatus.OnHold,
            TaskItemStatus.Completed => AssignmentStatus.Completed,
            TaskItemStatus.Overdue => AssignmentStatus.InProgress,
            _ => AssignmentStatus.NotStarted
        };
    }
}

/// <summary>
/// Abstraction to load subtasks without creating a circular dependency
/// between Application and Infrastructure layers.
/// </summary>
public interface ISubtaskLoader
{
    List<Subtask> LoadSubtasks(int assignmentId);
}

public class AssignmentSummary
{
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int OverdueAssignments { get; set; }
    public int DueSoonAssignments { get; set; }
}
