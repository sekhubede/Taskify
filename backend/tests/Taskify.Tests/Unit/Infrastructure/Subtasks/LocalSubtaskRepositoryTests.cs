using Taskify.Infrastructure.Storage;
using Taskify.Domain.Interfaces;

namespace Taskify.Tests.Unit.Infrastructure.Subtasks;

public class LocalSubtaskRepositoryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LocalSubtaskRepository _repo;

    public LocalSubtaskRepositoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "TaskifyTests", Guid.NewGuid().ToString("N"));
        var store = new SubtaskStore(Path.Combine(_tempDir, "subtasks"));
        var notes = new SubtaskNoteStore(Path.Combine(_tempDir, "notes"));
        _repo = new LocalSubtaskRepository(store, notes);
    }

    [Fact]
    public void Add_And_List_Subtasks_Works()
    {
        var assignmentId = 7;
        var s1 = _repo.AddSubtask(assignmentId, "A");
        var s2 = _repo.AddSubtask(assignmentId, "B");

        var list = _repo.GetSubtasksForAssignment(assignmentId);
        Assert.Equal(2, list.Count);
        Assert.Equal(new[] { s1.Id, s2.Id }, list.Select(x => x.Id).ToArray());
    }

    [Fact]
    public void ToggleCompletion_Updates_State()
    {
        var s = _repo.AddSubtask(123, "Task");
        Assert.False(s.IsCompleted);

        var ok = _repo.ToggleSubtaskCompletion(s.Id, true);
        Assert.True(ok);
        var after = _repo.GetSubtasksForAssignment(123).First();
        Assert.True(after.IsCompleted);

        ok = _repo.ToggleSubtaskCompletion(s.Id, false);
        Assert.True(ok);
        after = _repo.GetSubtasksForAssignment(123).First();
        Assert.False(after.IsCompleted);
    }

    [Fact]
    public void PersonalNotes_Save_And_Delete()
    {
        var s = _repo.AddSubtask(5, "Has note");
        _repo.UpdateSubtaskPersonalNote(s.Id, "note1");
        Assert.Equal("note1", _repo.GetSubtaskPersonalNote(s.Id));

        _repo.UpdateSubtaskPersonalNote(s.Id, null);
        Assert.Null(_repo.GetSubtaskPersonalNote(s.Id));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }
}


