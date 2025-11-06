using System.Text.Json;

namespace Taskify.Infrastructure.Storage;

public class WorkingOnStore
{
    private readonly string _storageFilePath;
    private HashSet<int> _workingOnIds; // assignment IDs that are marked as "working on"

    public WorkingOnStore(string storageDirectory = "storage")
    {
        // Normalize to an absolute path relative to the executable base directory for stability
        if (!Path.IsPathRooted(storageDirectory))
        {
            storageDirectory = Path.Combine(AppContext.BaseDirectory, storageDirectory);
        }

        if (!Directory.Exists(storageDirectory))
        {
            Directory.CreateDirectory(storageDirectory);
        }

        _storageFilePath = Path.Combine(storageDirectory, "working_on.json");
        _workingOnIds = LoadWorkingOn();
    }

    public void SetWorkingOn(int assignmentId, bool isWorkingOn)
    {
        if (isWorkingOn)
        {
            _workingOnIds.Add(assignmentId);
        }
        else
        {
            _workingOnIds.Remove(assignmentId);
        }
        PersistWorkingOn();
    }

    public bool IsWorkingOn(int assignmentId)
    {
        return _workingOnIds.Contains(assignmentId);
    }

    public HashSet<int> GetAllWorkingOn()
    {
        return new HashSet<int>(_workingOnIds);
    }

    private HashSet<int> LoadWorkingOn()
    {
        try
        {
            if (File.Exists(_storageFilePath))
            {
                var json = File.ReadAllText(_storageFilePath);
                var list = JsonSerializer.Deserialize<List<int>>(json);
                return list != null ? new HashSet<int>(list) : new HashSet<int>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load working on flags: {ex.Message}");
        }

        return new HashSet<int>();
    }

    private void PersistWorkingOn()
    {
        try
        {
            var list = _workingOnIds.ToList();
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_storageFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to persist working on flags: {ex.Message}");
        }
    }
}

