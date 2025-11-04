using Taskify.Domain.Entities;
using Taskify.Domain.Interfaces;

namespace Taskify.Application.Assignments.Services;

public class AssignmentService
{
    private readonly IAssignmentRepository _assignmentRepository;

    public AssignmentService(IAssignmentRepository assignmentRepository)
    {
        _assignmentRepository = assignmentRepository;
    }

    public List<Assignment> GetUserAssignments()
    {
        try
        {
            var assignments = _assignmentRepository.GetUserAssignments();

            // Sort by overdue first; within overdue, nearest due (most recent past) first.
            // Then non-overdue by nearest future due date; null due dates last.
            return assignments
                .OrderByDescending(a => a.IsOverdue())
                .ThenBy(a => a.IsOverdue()
                    ? -(a.DueDate?.Ticks ?? long.MinValue) // overdue: more recent past first
                    : (a.DueDate?.Ticks ?? long.MaxValue)) // non-overdue: earlier future first; nulls last
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving assignments: {ex.Message}");
            throw;
        }
    }

    public Assignment? GetAssignment(int assignmentId)
    {
        return _assignmentRepository.GetAssignmentById(assignmentId);
    }

    public bool CompleteAssignment(int assignmentId)
    {
        try
        {
            return _assignmentRepository.MarkAssignmentComplete(assignmentId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error completing assignment {assignmentId}: {ex.Message}");
            return false;
        }
    }

    public AssignmentSummary GetAssignmentSummary()
    {
        var assignments = GetUserAssignments();

        return new AssignmentSummary
        {
            TotalAssignments = assignments.Count,
            CompletedAssignments = assignments.Count(a => a.Status == AssignmentStatus.Completed),
            OverdueAssignments = assignments.Count(a => a.IsOverdue()),
            DueSoonAssignments = assignments.Count(a => a.IsDueSoon())
        };
    }
}

public class AssignmentSummary
{
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int OverdueAssignments { get; set; }
    public int DueSoonAssignments { get; set; }
}