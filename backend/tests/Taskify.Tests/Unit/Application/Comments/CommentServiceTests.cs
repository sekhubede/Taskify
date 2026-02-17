using Xunit;
using Moq;
using Taskify.Connectors;
using Taskify.Application.Comments.Services;
using Taskify.Domain.Entities;

namespace Taskify.Tests.Unit.Application.Comments;

public class CommentServiceTests
{
    private static CommentDTO MakeCommentDTO(
        int id,
        int assignmentId,
        string content = "content",
        string author = "Alice",
        DateTime? created = null)
    {
        return new CommentDTO
        {
            Id = id,
            Content = content,
            AuthorName = author,
            CreatedDate = created ?? DateTime.UtcNow,
            AssignmentId = assignmentId
        };
    }

    [Fact]
    public void GetAssignmentComments_Returns_Mapped_Comments_From_DataSource()
    {
        // Arrange
        var dtos = new List<CommentDTO>
        {
            MakeCommentDTO(1, 42, "c1"),
            MakeCommentDTO(2, 42, "c2")
        };

        var ds = new Mock<ITaskDataSource>();
        ds.Setup(r => r.GetCommentsForTaskAsync("42")).ReturnsAsync(dtos);

        var svc = new CommentService(ds.Object);

        // Act
        var result = svc.GetAssignmentComments(42);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("c1", result[0].Content);
        Assert.Equal("c2", result[1].Content);
        ds.Verify(r => r.GetCommentsForTaskAsync("42"), Times.Once);
    }

    [Fact]
    public void GetAssignmentComments_Propagates_Exception()
    {
        // Arrange
        var ds = new Mock<ITaskDataSource>();
        ds.Setup(r => r.GetCommentsForTaskAsync("7")).ThrowsAsync(new ApplicationException("boom"));
        var svc = new CommentService(ds.Object);

        // Act + Assert
        var ex = Assert.Throws<ApplicationException>(() => svc.GetAssignmentComments(7));
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public void AddComment_Validates_Content_NonEmpty()
    {
        // Arrange
        var ds = new Mock<ITaskDataSource>(MockBehavior.Strict);
        var svc = new CommentService(ds.Object);

        // Act + Assert
        Assert.Throws<ArgumentException>(() => svc.AddComment(1, " "));
        Assert.Throws<ArgumentException>(() => svc.AddComment(1, ""));
    }

    [Fact]
    public void AddComment_Validates_Content_Max_Length()
    {
        // Arrange
        var ds = new Mock<ITaskDataSource>(MockBehavior.Strict);
        var svc = new CommentService(ds.Object);

        var tooLong = new string('x', 5001);

        // Act + Assert
        Assert.Throws<ArgumentException>(() => svc.AddComment(1, tooLong));
    }

    [Fact]
    public void AddComment_Forwards_To_DataSource_When_Valid()
    {
        // Arrange
        var dto = MakeCommentDTO(10, 5, "hello", "Bob");
        var ds = new Mock<ITaskDataSource>();
        ds.Setup(r => r.AddCommentAsync("5", "hello")).ReturnsAsync(dto);

        var svc = new CommentService(ds.Object);

        // Act
        var result = svc.AddComment(5, "hello");

        // Assert
        Assert.Equal(10, result.Id);
        Assert.Equal("hello", result.Content);
        Assert.Equal("Bob", result.AuthorName);
        ds.Verify(r => r.AddCommentAsync("5", "hello"), Times.Once);
    }

    [Fact]
    public void GetCommentCount_Forwards_To_DataSource()
    {
        // Arrange
        var ds = new Mock<ITaskDataSource>();
        ds.Setup(r => r.GetCommentCountAsync("23")).ReturnsAsync(3);
        var svc = new CommentService(ds.Object);

        // Act
        var count = svc.GetCommentCount(23);

        // Assert
        Assert.Equal(3, count);
        ds.Verify(r => r.GetCommentCountAsync("23"), Times.Once);
    }

    [Fact]
    public void GetCommentSummary_Computes_Correct_Aggregates()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var dtos = new List<CommentDTO>
        {
            MakeCommentDTO(1, 9, "old", created: now.AddHours(-30)),
            MakeCommentDTO(2, 9, "recent", created: now.AddHours(-2)),
            MakeCommentDTO(3, 9, "recent2", created: now.AddHours(-10))
        };

        var ds = new Mock<ITaskDataSource>();
        ds.Setup(r => r.GetCommentsForTaskAsync("9")).ReturnsAsync(dtos);

        var svc = new CommentService(ds.Object);

        // Act
        var summary = svc.GetCommentSummary(9);

        // Assert
        Assert.Equal(3, summary.TotalComments);
        Assert.Equal(2, summary.RecentComments);
        Assert.NotNull(summary.LatestCommentDate);
    }
}
