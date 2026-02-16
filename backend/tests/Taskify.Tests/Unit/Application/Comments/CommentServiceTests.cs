using Xunit;
using Moq;
using Taskify.Application.Comments.Services;
using Taskify.Domain.Entities;
using Taskify.Domain.Interfaces;

namespace Taskify.Tests.Unit.Application.Comments;

public class CommentServiceTests
{
    private static Comment MakeComment(
        int id,
        int assignmentId,
        string content = "content",
        string author = "Alice",
        DateTime? created = null)
    {
        return new Comment(
            id: id,
            content: content,
            authorName: author,
            createdDate: created ?? DateTime.UtcNow,
            assignmentId: assignmentId);
    }

    [Fact]
    public void GetAssignmentComments_Forwards_To_Repository_And_Returns_List()
    {
        // Arrange
        var comments = new List<Comment>
        {
            MakeComment(1, 42, "c1"),
            MakeComment(2, 42, "c2")
        };

        var repo = new Mock<ICommentRepository>();
        repo.Setup(r => r.GetCommentsForAssignment(42)).Returns(comments);

        var svc = new CommentService(repo.Object);

        // Act
        var result = svc.GetAssignmentComments(42);

        // Assert
        Assert.Same(comments, result);
        repo.Verify(r => r.GetCommentsForAssignment(42), Times.Once);
    }

    [Fact]
    public void GetAssignmentComments_Propagates_Exception()
    {
        // Arrange
        var repo = new Mock<ICommentRepository>();
        repo.Setup(r => r.GetCommentsForAssignment(7)).Throws(new ApplicationException("boom"));
        var svc = new CommentService(repo.Object);

        // Act + Assert
        var ex = Assert.Throws<ApplicationException>(() => svc.GetAssignmentComments(7));
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public void AddComment_Validates_Content_NonEmpty()
    {
        // Arrange
        var repo = new Mock<ICommentRepository>(MockBehavior.Strict);
        var svc = new CommentService(repo.Object);

        // Act + Assert
        Assert.Throws<ArgumentException>(() => svc.AddComment(1, " ", "Author"));
        Assert.Throws<ArgumentException>(() => svc.AddComment(1, "", "Author"));
    }

    [Fact]
    public void AddComment_Validates_Content_Max_Length()
    {
        // Arrange
        var repo = new Mock<ICommentRepository>(MockBehavior.Strict);
        var svc = new CommentService(repo.Object);

        var tooLong = new string('x', 5001);

        // Act + Assert
        Assert.Throws<ArgumentException>(() => svc.AddComment(1, tooLong, "Author"));
    }

    [Fact]
    public void AddComment_Forwards_To_Repository_When_Valid()
    {
        // Arrange
        var expected = MakeComment(10, 5, "hello", "Bob");
        var repo = new Mock<ICommentRepository>();
        repo.Setup(r => r.AddComment(5, "hello", "Bob")).Returns(expected);

        var svc = new CommentService(repo.Object);

        // Act
        var result = svc.AddComment(5, "hello", "Bob");

        // Assert
        Assert.Same(expected, result);
        repo.Verify(r => r.AddComment(5, "hello", "Bob"), Times.Once);
    }

    [Fact]
    public void GetCommentCount_Forwards_To_Repository()
    {
        // Arrange
        var repo = new Mock<ICommentRepository>();
        repo.Setup(r => r.GetCommentCount(23)).Returns(3);
        var svc = new CommentService(repo.Object);

        // Act
        var count = svc.GetCommentCount(23);

        // Assert
        Assert.Equal(3, count);
        repo.Verify(r => r.GetCommentCount(23), Times.Once);
    }

    [Fact]
    public void GetCommentSummary_Computes_Correct_Aggregates()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var comments = new List<Comment>
        {
            MakeComment(1, 9, "old", created: now.AddHours(-30)),
            MakeComment(2, 9, "recent", created: now.AddHours(-2)),
            MakeComment(3, 9, "recent2", created: now.AddHours(-10))
        };

        var repo = new Mock<ICommentRepository>();
        repo.Setup(r => r.GetCommentsForAssignment(9)).Returns(comments);

        var svc = new CommentService(repo.Object);

        // Act
        var summary = svc.GetCommentSummary(9);

        // Assert
        Assert.Equal(3, summary.TotalComments);
        Assert.Equal(2, summary.RecentComments);
        Assert.Equal(comments.Max(c => c.CreatedDate), summary.LatestCommentDate);
    }
}
