namespace Taskify.Domain.Entities;

public class Subtask
{
    public int Id { get; private set; }
    public string Title { get; private set; }
    public bool IsCompleted { get; private set; }
    public int AssignmentId { get; private set; }
    public int Order { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime? CompletedDate { get; private set; }
    public string? PersonalNote { get; private set; }

    public Subtask(
        int id,
        string title,
        bool isCompleted,
        int assignmentId,
        int order,
        DateTime createdDate,
        DateTime? completedDate = null,
        string? personalNote = null)
    {
        if (id <= 0)
            throw new ArgumentException("Subtask ID must be positive", nameof(id));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentNullException(nameof(title), "Subtask title cannot be empty");

        if (assignmentId <= 0)
            throw new ArgumentException("Assignment ID must be positive", nameof(assignmentId));

        if (order < 0)
            throw new ArgumentException("Order cannot be negative", nameof(order));

        if (completedDate.HasValue && completedDate.Value > DateTime.UtcNow)
            throw new ArgumentException("Completed date cannot be in the future", nameof(completedDate));

        Id = id;
        Title = title;
        IsCompleted = isCompleted;
        AssignmentId = assignmentId;
        Order = order;
        CreatedDate = createdDate;
        CompletedDate = completedDate;
        PersonalNote = personalNote;
    }

    public void MarkAsComplete()
    {
        if (IsCompleted)
            throw new InvalidOperationException("Subtask is already completed");

        IsCompleted = true;
        CompletedDate = DateTime.UtcNow;
    }

    public void MarkAsIncomplete()
    {
        if (!IsCompleted)
            throw new InvalidOperationException("Subtask is already incomplete");

        IsCompleted = false;
        CompletedDate = null;
    }

    public void UpdatePersonalNote(string? note)
    {
        if (note != null && note.Length > 1000)
            throw new ArgumentException("Personal note cannot exceed 1000 characters");

        PersonalNote = note;
    }

    public bool HasPersonalNote()
    {
        return !string.IsNullOrWhiteSpace(PersonalNote);
    }
}