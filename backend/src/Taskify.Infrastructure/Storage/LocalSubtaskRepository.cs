using Taskify.Domain.Entities;
using Taskify.Domain.Interfaces;

namespace Taskify.Infrastructure.Storage;

public class LocalSubtaskRepository : ISubtaskRepository
{
    private readonly SubtaskStore _store;
    private readonly SubtaskNoteStore _noteStore;

    public LocalSubtaskRepository(SubtaskStore store, SubtaskNoteStore noteStore)
    {
        _store = store;
        _noteStore = noteStore;
    }

    public List<Subtask> GetSubtasksForAssignment(int assignmentId)
    {
        return _store.GetSubtasksForAssignment(assignmentId, _noteStore.GetNote);
    }

    public Subtask AddSubtask(int assignmentId, string title, int? order = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentNullException(nameof(title));

        if (assignmentId <= 0)
            throw new ArgumentException("Assignment ID must be positive", nameof(assignmentId));

        return _store.AddSubtask(assignmentId, title.Trim(), order, _noteStore.GetNote);
    }

    public bool ToggleSubtaskCompletion(int subtaskId, bool isCompleted)
    {
        return _store.SetCompletion(subtaskId, isCompleted);
    }

    public void UpdateSubtaskPersonalNote(int subtaskId, string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            _noteStore.DeleteNote(subtaskId);
        }
        else
        {
            _noteStore.SaveNote(subtaskId, note);
        }
    }

    public string? GetSubtaskPersonalNote(int subtaskId)
    {
        return _noteStore.GetNote(subtaskId);
    }

    public void ReorderSubtasks(int assignmentId, Dictionary<int, int> subtaskIdToOrder)
    {
        if (assignmentId <= 0)
            throw new ArgumentException("Assignment ID must be positive", nameof(assignmentId));

        if (subtaskIdToOrder == null || subtaskIdToOrder.Count == 0)
            throw new ArgumentException("Subtask order mapping cannot be empty", nameof(subtaskIdToOrder));

        _store.ReorderSubtasks(assignmentId, subtaskIdToOrder);
    }
}


