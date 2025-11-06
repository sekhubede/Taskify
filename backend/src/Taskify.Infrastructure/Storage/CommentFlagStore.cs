using System.Text.Json;

namespace Taskify.Infrastructure.Storage;

public class CommentFlagStore
{
    private readonly string _storageFilePath;
    private HashSet<int> _flaggedComments;

    public CommentFlagStore(string storageDirectory = "storage")
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

        _storageFilePath = Path.Combine(storageDirectory, "comment_flags.json");
        _flaggedComments = LoadFlags();
    }

    public void SetFlag(int commentId, bool isFlagged)
    {
        if (isFlagged)
        {
            _flaggedComments.Add(commentId);
        }
        else
        {
            _flaggedComments.Remove(commentId);
        }
        PersistFlags();
    }

    public bool IsFlagged(int commentId)
    {
        return _flaggedComments.Contains(commentId);
    }

    public HashSet<int> GetAllFlaggedComments()
    {
        return new HashSet<int>(_flaggedComments);
    }

    private HashSet<int> LoadFlags()
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
            Console.WriteLine($"Warning: Failed to load comment flags: {ex.Message}");
        }

        return new HashSet<int>();
    }

    private void PersistFlags()
    {
        try
        {
            var list = _flaggedComments.ToList();
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_storageFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to persist comment flags: {ex.Message}");
        }
    }
}

