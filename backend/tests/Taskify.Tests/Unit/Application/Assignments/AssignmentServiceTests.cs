using Xunit;
using Moq;
using Taskify.Connectors;
using Taskify.Application.Assignments.Services;
using Taskify.Domain.Entities;

namespace Taskify.Tests.Unit.Application.Assignments;

public class AssignmentServiceTests
{
    private static TaskDTO MakeTaskDTO(
        string id,
        string title,
        DateTime? dueDate,
        TaskItemStatus status = TaskItemStatus.InProgress,
        string assigneeName = "user",
        DateTime? created = null,
        DateTime? completed = null)
    {
        return new TaskDTO
        {
            Id = id,
            Title = title,
            Description = "",
            AssigneeName = assigneeName,
            AssigneeId = "1",
            Status = status,
            DueDate = dueDate,
            CreatedAt = created ?? DateTime.UtcNow.AddDays(-1),
            LastUpdatedAt = DateTime.UtcNow,
            CompletedAt = completed,
            SourceSystem = "Test",
            SourceId = id
        };
    }

    private static (Mock<ITaskDataSource> ds, Mock<ISubtaskLoader> loader) CreateMocks()
    {
        var ds = new Mock<ITaskDataSource>();
        var loader = new Mock<ISubtaskLoader>();
        loader.Setup(l => l.LoadSubtasks(It.IsAny<int>())).Returns(new List<Subtask>());
        return (ds, loader);
    }

    [Fact]
    public void GetUserAssignments_Sorts_Overdue_First_Then_By_Nearest_DueDate()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var (ds, loader) = CreateMocks();

        ds.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(new List<TaskDTO>
        {
            MakeTaskDTO("4", "due+5d", now.AddDays(5)),
            MakeTaskDTO("1", "overdue-10d", now.AddDays(-10)),
            MakeTaskDTO("5", "no-due", null),
            MakeTaskDTO("2", "due+1d", now.AddDays(1)),
            MakeTaskDTO("3", "overdue-1d", now.AddDays(-1))
        });

        var svc = new AssignmentService(ds.Object, loader.Object);

        // Act
        var result = svc.GetUserAssignments();

        // Assert: overdue first (nearest overdue first), then future (nearest first), then no due date
        Assert.Equal(new[] { 3, 1, 2, 4, 5 }, result.Select(a => a.Id).ToArray());
    }

    [Fact]
    public void GetUserAssignments_Propagates_Exception()
    {
        // Arrange
        var (ds, loader) = CreateMocks();
        ds.Setup(r => r.GetAllTasksAsync()).ThrowsAsync(new ApplicationException("boom"));
        var svc = new AssignmentService(ds.Object, loader.Object);

        // Act + Assert
        var ex = Assert.Throws<ApplicationException>(() => svc.GetUserAssignments());
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public void GetAssignment_Returns_Mapped_Assignment()
    {
        // Arrange
        var (ds, loader) = CreateMocks();
        var task = MakeTaskDTO("42", "x", DateTime.UtcNow.AddDays(2));
        ds.Setup(r => r.GetTaskByIdAsync("42")).ReturnsAsync(task);
        var svc = new AssignmentService(ds.Object, loader.Object);

        // Act
        var result = svc.GetAssignment(42);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result!.Id);
        Assert.Equal("x", result.Title);
        ds.Verify(r => r.GetTaskByIdAsync("42"), Times.Once);
    }

    [Fact]
    public void CompleteAssignment_Returns_True_When_DataSource_Succeeds()
    {
        // Arrange
        var (ds, loader) = CreateMocks();
        ds.Setup(r => r.UpdateTaskStatusAsync("7", TaskItemStatus.Completed)).ReturnsAsync(true);
        var svc = new AssignmentService(ds.Object, loader.Object);

        // Act
        var ok = svc.CompleteAssignment(7);

        // Assert
        Assert.True(ok);
        ds.Verify(r => r.UpdateTaskStatusAsync("7", TaskItemStatus.Completed), Times.Once);
    }

    [Fact]
    public void CompleteAssignment_Returns_False_When_DataSource_Throws()
    {
        // Arrange
        var (ds, loader) = CreateMocks();
        ds.Setup(r => r.UpdateTaskStatusAsync("7", TaskItemStatus.Completed))
            .ThrowsAsync(new InvalidOperationException("fail"));
        var svc = new AssignmentService(ds.Object, loader.Object);

        // Act
        var ok = svc.CompleteAssignment(7);

        // Assert
        Assert.False(ok);
    }

    [Fact]
    public void GetAssignmentSummary_Computes_Correct_Aggregates()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var (ds, loader) = CreateMocks();

        ds.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(new List<TaskDTO>
        {
            MakeTaskDTO("1", "completed", now.AddDays(-5), TaskItemStatus.Completed, completed: now.AddDays(-4)),
            MakeTaskDTO("2", "overdue", now.AddDays(-1), TaskItemStatus.InProgress),
            MakeTaskDTO("3", "due soon", now.AddDays(2), TaskItemStatus.InProgress),
            MakeTaskDTO("4", "not due soon", now.AddDays(10), TaskItemStatus.InProgress),
            MakeTaskDTO("5", "no due", null, TaskItemStatus.InProgress)
        });

        var svc = new AssignmentService(ds.Object, loader.Object);

        // Act
        var summary = svc.GetAssignmentSummary();

        // Assert
        Assert.Equal(5, summary.TotalAssignments);
        Assert.Equal(1, summary.CompletedAssignments);
        Assert.Equal(1, summary.OverdueAssignments);
        Assert.Equal(1, summary.DueSoonAssignments);
    }
}
