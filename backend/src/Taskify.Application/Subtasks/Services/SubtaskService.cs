using Taskify.Domain.Entities;
using Taskify.Domain.Interfaces;

namespace Taskify.Application.Subtasks.Services;

public class SubtaskService
{
    private readonly ISubtaskRepository _subtaskRepository;

    public SubtaskService(ISubtaskRepository subtaskRepository)
    {
        _subtaskRepository = subtaskRepository;
    }

    public List<Subtask> GetSubtasksForAssignment(int assignmentId)
    {
        try
        {
            return _subtaskRepository.GetSubtasksForAssignment(assignmentId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving subtasks for assignment {assignmentId}: {ex.Message}");
            throw;
        }
    }

    public Subtask AddSubtask(int assignmentId, string title, int? order = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentNullException(nameof(title), "Subtask title cannot be empty");

        title = title.Trim();
        if (title.Length > 200)
            throw new ArgumentException("Subtask title cannot exceed 200 characters", nameof(title));

        return _subtaskRepository.AddSubtask(assignmentId, title, order);
    }

    public bool ToggleSubtaskCompletion(int subtaskId, bool isCompleted)
    {
        return _subtaskRepository.ToggleSubtaskCompletion(subtaskId, isCompleted);
    }

    public bool CompleteSubtask(int subtaskId)
    {
        return _subtaskRepository.ToggleSubtaskCompletion(subtaskId, isCompleted: true);
    }

    public bool IncompleteSubtask(int subtaskId)
    {
        return _subtaskRepository.ToggleSubtaskCompletion(subtaskId, isCompleted: false);
    }

    public void AddPersonalNote(int subtaskId, string note)
    {
        if (note.Length > 1000)
            throw new ArgumentException("Personal note cannot exceed 1000 characters");

        _subtaskRepository.UpdateSubtaskPersonalNote(subtaskId, note);
    }

    public void RemovePersonalNote(int subtaskId)
    {
        _subtaskRepository.UpdateSubtaskPersonalNote(subtaskId, null);
    }

    public string? GetPersonalNote(int subtaskId)
    {
        return _subtaskRepository.GetSubtaskPersonalNote(subtaskId);
    }

    public void ReorderSubtasks(int assignmentId, Dictionary<int, int> subtaskIdToOrder)
    {
        if (subtaskIdToOrder == null || subtaskIdToOrder.Count == 0)
            throw new ArgumentException("Subtask order mapping cannot be empty", nameof(subtaskIdToOrder));

        _subtaskRepository.ReorderSubtasks(assignmentId, subtaskIdToOrder);
    }

    public SubtaskSummary GetSubtaskSummary(int assignmentId)
    {
        var subtasks = GetSubtasksForAssignment(assignmentId);

        return new SubtaskSummary
        {
            TotalSubtasks = subtasks.Count,
            CompletedSubtasks = subtasks.Count(s => s.IsCompleted),
            PendingSubtasks = subtasks.Count(s => !s.IsCompleted),
            CompletionPercentage = subtasks.Any()
                ? (int)((double)subtasks.Count(s => s.IsCompleted) / subtasks.Count * 100)
                : 0,
            SubtasksWithNotes = subtasks.Count(s => s.HasPersonalNote())
        };
    }
}

public class SubtaskSummary
{
    public int TotalSubtasks { get; set; }
    public int CompletedSubtasks { get; set; }
    public int PendingSubtasks { get; set; }
    public int CompletionPercentage { get; set; }
    public int SubtasksWithNotes { get; set; }
}