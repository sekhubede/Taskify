using Taskify.Domain.Entities;

namespace Taskify.Domain.Interfaces;

public interface IAssignmentRepository
{
    /// <summary>
    /// Get all assignments for the current M-Files user
    /// </summary>
    List<Assignment> GetUserAssignments();

    /// <summary>
    /// Get an assignment by its ID
    /// </summary>
    Assignment? GetAssignmentById(int assignmentId);

    /// <summary>
    /// Mark an assignment as complete in M-Files
    /// </summary>
    bool MarkAssignmentComplete(int assignmentId);
}