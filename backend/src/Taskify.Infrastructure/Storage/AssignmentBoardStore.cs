using System.Text.Json;

namespace Taskify.Infrastructure.Storage;

public class AssignmentBoardStore
{
    private readonly string _storageFilePath;
    private Dictionary<int, string> _boardPositions; // assignmentId -> column name

    public AssignmentBoardStore(string storageDirectory = "storage")
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

        _storageFilePath = Path.Combine(storageDirectory, "assignment_board.json");
        _boardPositions = LoadPositions();
    }

    public void SetBoardPosition(int assignmentId, string column)
    {
        if (string.IsNullOrWhiteSpace(column))
        {
            _boardPositions.Remove(assignmentId);
        }
        else
        {
            _boardPositions[assignmentId] = column;
        }
        PersistPositions();
    }

    public string? GetBoardPosition(int assignmentId)
    {
        return _boardPositions.TryGetValue(assignmentId, out var position) ? position : null;
    }

    public Dictionary<int, string> GetAllPositions()
    {
        return new Dictionary<int, string>(_boardPositions);
    }

    private Dictionary<int, string> LoadPositions()
    {
        try
        {
            if (File.Exists(_storageFilePath))
            {
                var json = File.ReadAllText(_storageFilePath);
                return JsonSerializer.Deserialize<Dictionary<int, string>>(json)
                    ?? new Dictionary<int, string>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load assignment board positions: {ex.Message}");
        }

        return new Dictionary<int, string>();
    }

    private void PersistPositions()
    {
        try
        {
            var json = JsonSerializer.Serialize(_boardPositions, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_storageFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to persist assignment board positions: {ex.Message}");
        }
    }
}

