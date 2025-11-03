using Xunit;
using Moq;
using Taskify.Application.Assignments.Services;
using Taskify.Domain.Entities;
using Taskify.Domain.Interfaces;

namespace Taskify.Tests.Unit.Application.Assignments;

public class AssignmentServiceTests
{
    private static Assignment MakeAssignment(
        int id,
        string title,
        DateTime? dueDate,
        AssignmentStatus status = AssignmentStatus.InProgress,
        string assignedTo = "user",
        DateTime? created = null,
        DateTime? completed = null)
    {
        return new Assignment(
            id: id,
            title: title,
            description: "",
            dueDate: dueDate,
            status: status,
            assignedTo: assignedTo,
            createdDate: created ?? DateTime.UtcNow.AddDays(-1),
            completedDate: completed
        );
    }

    [Fact]
    public void GetUserAssignments_Sorts_Overdue_First_Then_By_Nearest_DueDate()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var overdueFar = MakeAssignment(1, "overdue-10d", now.AddDays(-10));
        var dueSoonTomorrow = MakeAssignment(2, "due+1d", now.AddDays(1));
        var overdueNear = MakeAssignment(3, "overdue-1d", now.AddDays(-1));
        var dueLater = MakeAssignment(4, "due+5d", now.AddDays(5));
        var noDue = MakeAssignment(5, "no-due", null);

        var list = new List<Assignment> { dueLater, overdueFar, noDue, dueSoonTomorrow, overdueNear };

        var repo = new Mock<IAssignmentRepository>();
        repo.Setup(r => r.GetUserAssignments()).Returns(list);

        var svc = new AssignmentService(repo.Object);

        // Act
        var result = svc.GetUserAssignments();

        // Assert
        Assert.Equal(new[] { 3, 1, 2, 4, 5 }, result.Select(a => a.Id).ToArray());
    }

    [Fact]
    public void GetUserAssignments_Propagates_Exception()
    {
        // Arrange
        var repo = new Mock<IAssignmentRepository>();
        repo.Setup(r => r.GetUserAssignments()).Throws(new ApplicationException("boom"));
        var svc = new AssignmentService(repo.Object);

        // Act + Assert
        var ex = Assert.Throws<ApplicationException>(() => svc.GetUserAssignments());
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public void GetAssignment_Forwards_To_Repository()
    {
        // Arrange
        var expected = MakeAssignment(42, "x", DateTime.UtcNow.AddDays(2));
        var repo = new Mock<IAssignmentRepository>();
        repo.Setup(r => r.GetAssignmentById(42)).Returns(expected);
        var svc = new AssignmentService(repo.Object);

        // Act
        var result = svc.GetAssignment(42);

        // Assert
        Assert.Same(expected, result);
        repo.Verify(r => r.GetAssignmentById(42), Times.Once);
    }

    [Fact]
    public void CompleteAssignment_Returns_True_When_Repository_Succeeds()
    {
        // Arrange
        var repo = new Mock<IAssignmentRepository>();
        repo.Setup(r => r.MarkAssignmentComplete(7)).Returns(true);
        var svc = new AssignmentService(repo.Object);

        // Act
        var ok = svc.CompleteAssignment(7);

        // Assert
        Assert.True(ok);
        repo.Verify(r => r.MarkAssignmentComplete(7), Times.Once);
    }

    [Fact]
    public void CompleteAssignment_Returns_False_When_Repository_Throws()
    {
        // Arrange
        var repo = new Mock<IAssignmentRepository>();
        repo.Setup(r => r.MarkAssignmentComplete(7)).Throws(new InvalidOperationException("fail"));
        var svc = new AssignmentService(repo.Object);

        // Act
        var ok = svc.CompleteAssignment(7);

        // Assert
        Assert.False(ok);
        repo.Verify(r => r.MarkAssignmentComplete(7), Times.Once);
    }

    [Fact]
    public void GetAssignmentSummary_Computes_Correct_Aggregates()
    {
        // Arrange
        var now = DateTime.UtcNow;

        var a1 = MakeAssignment(1, "completed", now.AddDays(-5), AssignmentStatus.Completed, completed: now.AddDays(-4));
        var a2 = MakeAssignment(2, "overdue", now.AddDays(-1), AssignmentStatus.InProgress);
        var a3 = MakeAssignment(3, "due soon", now.AddDays(2), AssignmentStatus.InProgress);
        var a4 = MakeAssignment(4, "not due soon", now.AddDays(10), AssignmentStatus.InProgress);
        var a5 = MakeAssignment(5, "no due", null, AssignmentStatus.InProgress);

        var repo = new Mock<IAssignmentRepository>();
        repo.Setup(r => r.GetUserAssignments()).Returns(new List<Assignment> { a1, a2, a3, a4, a5 });

        var svc = new AssignmentService(repo.Object);

        // Act
        var summary = svc.GetAssignmentSummary();

        // Assert
        Assert.Equal(5, summary.TotalAssignments);
        Assert.Equal(1, summary.CompletedAssignments);    // a1
        Assert.Equal(1, summary.OverdueAssignments);      // a2
        Assert.Equal(1, summary.DueSoonAssignments);      // a3 (2 days)
    }
}