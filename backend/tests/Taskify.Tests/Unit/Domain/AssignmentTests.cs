using Xunit;
using Taskify.Domain.Entities;

namespace Taskify.Tests.Unit.Domain;

public class AssignmentTests
{
    [Fact]
    public void Constructor_With_Valid_Parameters_Creates_Instance()
    {
        var due = DateTime.UtcNow.AddDays(3);
        var created = DateTime.UtcNow.AddDays(-1);

        var a = new Assignment(
            id: 1,
            title: "Title",
            description: "Desc",
            dueDate: due,
            status: AssignmentStatus.InProgress,
            assignedTo: "user",
            createdDate: created,
            completedDate: null
        );

        Assert.Equal(1, a.Id);
        Assert.Equal("Title", a.Title);
        Assert.Equal("user", a.AssignedTo);
        Assert.Equal(AssignmentStatus.InProgress, a.Status);
        Assert.Equal(due, a.DueDate);
        Assert.Equal(created, a.CreatedDate);
        Assert.Null(a.CompletedDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_Id_Must_Be_Positive(int id)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Assignment(id, "Title", "", null, AssignmentStatus.InProgress, "user", DateTime.UtcNow, null));
        Assert.Contains("Assignment ID must be greater than 0", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Title_Required(string title)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Assignment(1, title!, "", null, AssignmentStatus.InProgress, "user", DateTime.UtcNow, null));
        Assert.Contains("Assignment title cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_AssignedTo_Required(string assignedTo)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Assignment(1, "Title", "", null, AssignmentStatus.InProgress, assignedTo!, DateTime.UtcNow, null));
        Assert.Contains("Assignment assigned to cannot be empty", ex.Message);
    }

    [Fact]
    public void Constructor_CompletedDate_Cannot_Be_In_Future()
    {
        var future = DateTime.UtcNow.AddMinutes(1);
        var ex = Assert.Throws<ArgumentException>(() =>
            new Assignment(1, "Title", "", null, AssignmentStatus.InProgress, "user", DateTime.UtcNow, future));
        Assert.Contains("completed date cannot be in the future", ex.Message);
    }

    [Fact]
    public void MarkAsCompleted_Sets_Status_And_CompletedDate()
    {
        var a = new Assignment(1, "Title", "", null, AssignmentStatus.InProgress, "user", DateTime.UtcNow, null);

        a.MarkAsCompleted();

        Assert.Equal(AssignmentStatus.Completed, a.Status);
        Assert.True(a.CompletedDate.HasValue);
        Assert.True((DateTime.UtcNow - a.CompletedDate.Value).TotalSeconds < 5);
    }

    [Fact]
    public void MarkAsCompleted_When_Already_Completed_Throws()
    {
        var a = new Assignment(1, "Title", "", null, AssignmentStatus.Completed, "user", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(-1));
        var ex = Assert.Throws<InvalidOperationException>(() => a.MarkAsCompleted());
        Assert.Equal("Assignment is already completed", ex.Message);
    }

    [Theory]
    [InlineData(-1, true)]   // overdue
    [InlineData(0, false)]   // due now is not overdue
    [InlineData(1, false)]   // future
    public void IsOverdue_Behavior(int daysOffset, bool expected)
    {
        var due = DateTime.UtcNow.AddDays(daysOffset);
        var a = new Assignment(1, "Title", "", due, AssignmentStatus.InProgress, "user", DateTime.UtcNow, null);

        Assert.Equal(expected, a.IsOverdue());
    }

    [Theory]
    [InlineData(1, true)]   // within 3 days
    [InlineData(3, true)]   // exactly threshold
    [InlineData(4, false)]  // beyond threshold
    public void IsDueSoon_Default_Threshold(int daysAhead, bool expected)
    {
        var due = DateTime.UtcNow.AddDays(daysAhead);
        var a = new Assignment(1, "Title", "", due, AssignmentStatus.InProgress, "user", DateTime.UtcNow, null);

        Assert.Equal(expected, a.IsDueSoon());
    }

    [Fact]
    public void IsDueSoon_Ignores_Completed_Assignments()
    {
        var due = DateTime.UtcNow.AddDays(1);
        var a = new Assignment(1, "Title", "", due, AssignmentStatus.Completed, "user", DateTime.UtcNow, DateTime.UtcNow);

        Assert.False(a.IsDueSoon());
    }
}