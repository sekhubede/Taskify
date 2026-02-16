using Xunit;
using Moq;
using Taskify.Connectors;
using Taskify.Application.Assignments.Services;
using Taskify.Domain.Entities;

namespace Taskify.Tests.Unit.Application.Connector;

public class ConnectorIntegrationTests
{
    [Fact]
    public void GetUserAssignments_Should_Return_Mapped_Assignments_From_DataSource()
    {
        // Arrange
        var mockDataSource = new Mock<ITaskDataSource>();
        var mockSubtaskLoader = new Mock<ISubtaskLoader>();

        mockDataSource.Setup(ds => ds.GetAllTasksAsync())
            .ReturnsAsync(new List<TaskDTO>
            {
                new TaskDTO
                {
                    Id = "1",
                    Title = "Test Assignment",
                    Description = "Test Description",
                    AssigneeName = "Test User",
                    Status = TaskItemStatus.InProgress,
                    CreatedAt = DateTime.UtcNow
                }
            });

        mockSubtaskLoader.Setup(l => l.LoadSubtasks(It.IsAny<int>()))
            .Returns(new List<Subtask>());

        var service = new AssignmentService(mockDataSource.Object, mockSubtaskLoader.Object);

        // Act
        var assignments = service.GetUserAssignments();

        // Assert
        Assert.Single(assignments);
        Assert.Equal("Test Assignment", assignments[0].Title);
        Assert.Equal(AssignmentStatus.InProgress, assignments[0].Status);
    }

    [Fact]
    public void CompleteAssignment_Should_Call_UpdateTaskStatus_On_DataSource()
    {
        // Arrange
        var mockDataSource = new Mock<ITaskDataSource>();
        var mockSubtaskLoader = new Mock<ISubtaskLoader>();

        mockDataSource.Setup(ds => ds.UpdateTaskStatusAsync("1", TaskItemStatus.Completed))
            .ReturnsAsync(true);

        var service = new AssignmentService(mockDataSource.Object, mockSubtaskLoader.Object);

        // Act
        var result = service.CompleteAssignment(1);

        // Assert
        Assert.True(result);
        mockDataSource.Verify(ds => ds.UpdateTaskStatusAsync("1", TaskItemStatus.Completed), Times.Once());
    }

    [Fact]
    public void GetAssignment_Should_Return_Null_When_DataSource_Returns_Null()
    {
        // Arrange
        var mockDataSource = new Mock<ITaskDataSource>();
        var mockSubtaskLoader = new Mock<ISubtaskLoader>();

        mockDataSource.Setup(ds => ds.GetTaskByIdAsync("999"))
            .ReturnsAsync((TaskDTO?)null);

        var service = new AssignmentService(mockDataSource.Object, mockSubtaskLoader.Object);

        // Act
        var result = service.GetAssignment(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetUserAssignments_Should_Order_Overdue_First()
    {
        // Arrange
        var mockDataSource = new Mock<ITaskDataSource>();
        var mockSubtaskLoader = new Mock<ISubtaskLoader>();

        mockDataSource.Setup(ds => ds.GetAllTasksAsync())
            .ReturnsAsync(new List<TaskDTO>
            {
                new TaskDTO
                {
                    Id = "1",
                    Title = "Future Task",
                    Description = "",
                    AssigneeName = "User",
                    Status = TaskItemStatus.Open,
                    DueDate = DateTime.UtcNow.AddDays(5),
                    CreatedAt = DateTime.UtcNow
                },
                new TaskDTO
                {
                    Id = "2",
                    Title = "Overdue Task",
                    Description = "",
                    AssigneeName = "User",
                    Status = TaskItemStatus.Open,
                    DueDate = DateTime.UtcNow.AddDays(-3),
                    CreatedAt = DateTime.UtcNow
                }
            });

        mockSubtaskLoader.Setup(l => l.LoadSubtasks(It.IsAny<int>()))
            .Returns(new List<Subtask>());

        var service = new AssignmentService(mockDataSource.Object, mockSubtaskLoader.Object);

        // Act
        var assignments = service.GetUserAssignments();

        // Assert
        Assert.Equal(2, assignments.Count);
        Assert.Equal("Overdue Task", assignments[0].Title);
        Assert.Equal("Future Task", assignments[1].Title);
    }
}
