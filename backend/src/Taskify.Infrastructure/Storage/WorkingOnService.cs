using Taskify.Infrastructure.Storage;

namespace Taskify.Infrastructure.Storage;

public class WorkingOnService
{
    private readonly WorkingOnStore _workingOnStore;

    public WorkingOnService(WorkingOnStore workingOnStore)
    {
        _workingOnStore = workingOnStore;
    }

    public bool IsWorkingOn(int assignmentId)
    {
        return _workingOnStore.IsWorkingOn(assignmentId);
    }

    public void SetWorkingOn(int assignmentId, bool isWorkingOn)
    {
        _workingOnStore.SetWorkingOn(assignmentId, isWorkingOn);
    }

    public HashSet<int> GetAllWorkingOn()
    {
        return _workingOnStore.GetAllWorkingOn();
    }
}

