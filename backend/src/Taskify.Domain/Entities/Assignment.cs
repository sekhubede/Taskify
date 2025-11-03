namespace Taskify.Domain.Entities;

public class Assignment
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime? DueDate { get; set; }
    public AssignmentStatus Status { get; set; }
    public string AssignedTo { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    public Assignment(int id, string title, string description, DateTime? dueDate, AssignmentStatus status, string assignedTo, DateTime? createdDate, DateTime? completedDate)
    {
        if (id <= 0)
            throw new ArgumentException("Assignment ID must be greater than 0", nameof(id));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Assignment title cannot be empty", nameof(title));

        if (string.IsNullOrWhiteSpace(assignedTo))
            throw new ArgumentException("Assignment assigned to cannot be empty", nameof(assignedTo));

        if (completedDate.HasValue && completedDate.Value > DateTime.UtcNow)
            throw new ArgumentException("Assignment completed date cannot be in the future", nameof(completedDate));

        Id = id;
        Title = title;
        Description = description;
        DueDate = dueDate;
        Status = status;
        AssignedTo = assignedTo;
        CreatedDate = createdDate;
        CompletedDate = completedDate;
    }

    public void MarkAsCompleted()
    {
        if (Status == AssignmentStatus.Completed)
            throw new InvalidOperationException("Assignment is already completed");

        Status = AssignmentStatus.Completed;
        CompletedDate = DateTime.UtcNow;
    }

    public bool IsOverdue()
    {
        if (Status == AssignmentStatus.Completed || !DueDate.HasValue)
            return false;

        // Only dates before today are overdue; "due today" is not overdue
        return DueDate.Value.Date < DateTime.UtcNow.Date;
    }

    public bool IsDueSoon(int daysThreshold = 3)
    {
        if (!DueDate.HasValue || Status == AssignmentStatus.Completed)
            return false;

        var daysUntilDue = (DueDate.Value - DateTime.UtcNow).TotalDays;
        return daysUntilDue > 0 && daysUntilDue <= daysThreshold;
    }

}
public enum AssignmentStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    OnHold = 3,
    WaitingForInformation = 4,
    WaitingForFeedback = 5,
    WaitingForReview = 6
}