using Taskify.Domain.Entities;

namespace Taskify.Domain.Interfaces;

public interface ISubtaskRepository
{
    /// <summary>
    /// Retrieves all subtasks for a specific assignment from local storage.
    /// </summary>
    List<Subtask> GetSubtasksForAssignment(int assignmentId);

    /// <summary>
    /// Adds a new local subtask to the specified assignment (does not persist to M-Files).
    /// </summary>
    Subtask AddSubtask(int assignmentId, string title, int? order = null);

    /// <summary>
    /// Toggles the completion status of a local subtask.
    /// </summary>
    bool ToggleSubtaskCompletion(int subtaskId, bool isCompleted);

    /// <summary>
    /// Updates the personal note for a subtask (stored locally, not in M-Files).
    /// </summary>
    void UpdateSubtaskPersonalNote(int subtaskId, string? note);

    /// <summary>
    /// Gets the personal note for a subtask (from local storage).
    /// </summary>
    string? GetSubtaskPersonalNote(int subtaskId);
}