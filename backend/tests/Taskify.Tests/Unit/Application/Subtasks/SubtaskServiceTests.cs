using Moq;
using Taskify.Application.Subtasks.Services;
using Taskify.Domain.Entities;
using Taskify.Domain.Interfaces;

namespace Taskify.Tests.Unit.Application.Subtasks;

public class SubtaskServiceTests
{
    [Fact]
    public void AddSubtask_Forwards_To_Repository()
    {
        var repo = new Mock<ISubtaskRepository>();
        var service = new SubtaskService(repo.Object);

        repo.Setup(r => r.AddSubtask(1, "Title", null))
            .Returns(new Subtask(10, "Title", false, 1, 0, DateTime.UtcNow));

        var created = service.AddSubtask(1, "Title");

        Assert.Equal(10, created.Id);
        repo.Verify(r => r.AddSubtask(1, "Title", null), Times.Once);
    }

    [Fact]
    public void AddPersonalNote_Validates_Length()
    {
        var repo = new Mock<ISubtaskRepository>();
        var service = new SubtaskService(repo.Object);

        var longNote = new string('x', 1001);
        Assert.Throws<ArgumentException>(() => service.AddPersonalNote(5, longNote));
        repo.Verify(r => r.UpdateSubtaskPersonalNote(It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public void ToggleCompletion_Forwards_To_Repository()
    {
        var repo = new Mock<ISubtaskRepository>();
        repo.Setup(r => r.ToggleSubtaskCompletion(9, true)).Returns(true);

        var service = new SubtaskService(repo.Object);
        var ok = service.ToggleSubtaskCompletion(9, true);

        Assert.True(ok);
        repo.Verify(r => r.ToggleSubtaskCompletion(9, true), Times.Once);
    }
}


