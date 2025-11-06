using Taskify.Infrastructure.Storage;

namespace Taskify.Infrastructure.Storage;

public class AssignmentBoardService
{
    private readonly AssignmentBoardStore _boardStore;

    public AssignmentBoardService(AssignmentBoardStore boardStore)
    {
        _boardStore = boardStore;
    }

    public string? GetAssignmentColumn(int assignmentId)
    {
        return _boardStore.GetBoardPosition(assignmentId);
    }

    public void SetAssignmentColumn(int assignmentId, string? column)
    {
        _boardStore.SetBoardPosition(assignmentId, column ?? string.Empty);
    }

    public Dictionary<int, string> GetAllPositions()
    {
        return _boardStore.GetAllPositions();
    }
}

