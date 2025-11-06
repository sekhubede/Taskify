using System.Text.Json;

namespace Taskify.Infrastructure.Storage;

public class CommentNoteStore
{
    private readonly string _storageFilePath;
    private Dictionary<int, string> _notes;

    public CommentNoteStore(string storageDirectory = "storage")
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

        _storageFilePath = Path.Combine(storageDirectory, "comment_notes.json");
        _notes = LoadNotes();
    }

    public void SaveNote(int commentId, string note)
    {
        _notes[commentId] = note;
        PersistNotes();
    }

    public void DeleteNote(int commentId)
    {
        _notes.Remove(commentId);
        PersistNotes();
    }

    public string? GetNote(int commentId)
    {
        return _notes.TryGetValue(commentId, out var note) ? note : null;
    }

    public Dictionary<int, string> GetAllNotes()
    {
        return new Dictionary<int, string>(_notes);
    }

    private Dictionary<int, string> LoadNotes()
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
            Console.WriteLine($"Warning: Failed to load comment notes: {ex.Message}");
        }

        return new Dictionary<int, string>();
    }

    private void PersistNotes()
    {
        try
        {
            var json = JsonSerializer.Serialize(_notes, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_storageFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to persist comment notes: {ex.Message}");
        }
    }
}

