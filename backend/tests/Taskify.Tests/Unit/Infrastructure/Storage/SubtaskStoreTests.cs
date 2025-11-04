using Taskify.Infrastructure.Storage;
using Taskify.Domain.Entities;

namespace Taskify.Tests.Unit.Infrastructure.Storage;

public class SubtaskStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SubtaskNoteStore _noteStore;
    private readonly SubtaskStore _store;

    public SubtaskStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "TaskifyTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _noteStore = new SubtaskNoteStore(Path.Combine(_tempDir, "notes"));
        _store = new SubtaskStore(Path.Combine(_tempDir, "subtasks"));
    }

    [Fact]
    public void AddSubtask_Persists_And_CanBeRetrieved_WithNote()
    {
        var assignmentId = 123;

        var created = _store.AddSubtask(assignmentId, "Write tests", null, _noteStore.GetNote);
        Assert.NotNull(created);
        Assert.Equal(assignmentId, created.AssignmentId);
        Assert.Equal("Write tests", created.Title);
        Assert.False(created.IsCompleted);

        _noteStore.SaveNote(created.Id, "my note");

        var list = _store.GetSubtasksForAssignment(assignmentId, _noteStore.GetNote);
        Assert.Single(list);
        Assert.Equal("my note", list[0].PersonalNote);
    }

    [Fact]
    public void ToggleCompletion_SetsCompletedDate_And_Unsets()
    {
        var assignmentId = 55;
        var created = _store.AddSubtask(assignmentId, "Do thing", null, _noteStore.GetNote);

        var ok = _store.SetCompletion(created.Id, true);
        Assert.True(ok);

        var after = _store.GetSubtasksForAssignment(assignmentId, _noteStore.GetNote).First();
        Assert.True(after.IsCompleted);
        Assert.NotNull(after.CompletedDate);

        ok = _store.SetCompletion(created.Id, false);
        Assert.True(ok);
        after = _store.GetSubtasksForAssignment(assignmentId, _noteStore.GetNote).First();
        Assert.False(after.IsCompleted);
        Assert.Null(after.CompletedDate);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // ignore
        }
    }
}


