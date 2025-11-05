using System.Text.Json;

namespace Taskify.Infrastructure.Storage;

public class SubtaskNoteStore
{
    private readonly string _storageFilePath;
    private Dictionary<int, string> _notes;

    public SubtaskNoteStore(string storageDirectory = "storage")
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

        _storageFilePath = Path.Combine(storageDirectory, "subtask_notes.json");
        _notes = LoadNotes();
    }

    public void SaveNote(int subtaskId, string note)
    {
        _notes[subtaskId] = note;
        PersistNotes();
    }

    public void DeleteNote(int subtaskId)
    {
        _notes.Remove(subtaskId);
        PersistNotes();
    }

    public string? GetNote(int subtaskId)
    {
        return _notes.TryGetValue(subtaskId, out var note) ? note : null;
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
            Console.WriteLine($"Warning: Failed to load personal notes: {ex.Message}");
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
            Console.WriteLine($"Warning: Failed to persist personal notes: {ex.Message}");
        }
    }
}