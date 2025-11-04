using Xunit;
using Taskify.Domain.Entities;

namespace Taskify.Tests.Unit.Domain;

public class CommentTests
{
    [Fact]
    public void Constructor_Valid_Data_Creates_Instance()
    {
        var now = DateTime.UtcNow;
        var c = new Comment(1, "hello", "Alice", now, 7);

        Assert.Equal(1, c.Id);
        Assert.Equal("hello", c.Content);
        Assert.Equal("Alice", c.AuthorName);
        Assert.Equal(now, c.CreatedDate);
        Assert.Equal(7, c.AssignmentId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_Invalid_Id_Throws(int id)
    {
        Assert.Throws<ArgumentException>(() => new Comment(id, "x", "a", DateTime.UtcNow, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_Invalid_AssignmentId_Throws(int assignmentId)
    {
        Assert.Throws<ArgumentException>(() => new Comment(1, "x", "a", DateTime.UtcNow, assignmentId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Empty_Content_Throws(string content)
    {
        Assert.Throws<ArgumentNullException>(() => new Comment(1, content, "a", DateTime.UtcNow, 1));
    }

    [Fact]
    public void Constructor_Content_Too_Long_Throws()
    {
        var longText = new string('x', 5001);
        Assert.Throws<ArgumentException>(() => new Comment(1, longText, "a", DateTime.UtcNow, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Empty_Author_Throws(string author)
    {
        Assert.Throws<ArgumentNullException>(() => new Comment(1, "x", author, DateTime.UtcNow, 1));
    }

    [Fact]
    public void IsRecentlyAdded_Uses_24h_Threshold_By_Default()
    {
        var recent = new Comment(1, "x", "a", DateTime.UtcNow.AddHours(-3), 1);
        var old = new Comment(2, "x", "a", DateTime.UtcNow.AddHours(-30), 1);

        Assert.True(recent.IsRecentlyAdded());
        Assert.False(old.IsRecentlyAdded());
    }

    [Fact]
    public void GetContentPreview_Truncates_With_Ellipsis_When_Too_Long()
    {
        var content = new string('x', 120);
        var c = new Comment(1, content, "a", DateTime.UtcNow, 1);

        var preview = c.GetContentPreview(100);

        Assert.Equal(103, preview.Length); // 100 + "..."
        Assert.EndsWith("...", preview);
    }

    [Fact]
    public void GetContentPreview_Returns_Full_When_Short_Enough()
    {
        var c = new Comment(1, "short", "a", DateTime.UtcNow, 1);
        Assert.Equal("short", c.GetContentPreview(100));
    }
}


