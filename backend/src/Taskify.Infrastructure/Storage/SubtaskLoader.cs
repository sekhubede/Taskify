using Taskify.Application.Assignments.Services;
using Taskify.Domain.Entities;
using Taskify.Domain.Interfaces;

namespace Taskify.Infrastructure.Storage;

/// <summary>
/// Bridges the ISubtaskLoader abstraction to the existing ISubtaskRepository.
/// </summary>
public class SubtaskLoader : ISubtaskLoader
{
    private readonly ISubtaskRepository _subtaskRepository;

    public SubtaskLoader(ISubtaskRepository subtaskRepository)
    {
        _subtaskRepository = subtaskRepository;
    }

    public List<Subtask> LoadSubtasks(int assignmentId)
    {
        try
        {
            return _subtaskRepository.GetSubtasksForAssignment(assignmentId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load subtasks for assignment {assignmentId}: {ex.Message}");
            return new List<Subtask>();
        }
    }
}
