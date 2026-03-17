using System.Text.Json;

namespace Taskify.Infrastructure.Storage;

public class SubtaskNoteStore
{
    private readonly string _storageFilePath;
    private Dictionary<int, SubtaskNoteItem> _notes;

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

    public SubtaskNoteItem SaveNote(int subtaskId, string note)
    {
        var now = DateTime.UtcNow;
        if (_notes.TryGetValue(subtaskId, out var existing))
        {
            existing.Note = note;
            existing.UpdatedDate = now;
            _notes[subtaskId] = existing;
        }
        else
        {
            _notes[subtaskId] = new SubtaskNoteItem
            {
                Note = note,
                CreatedDate = now,
                UpdatedDate = now
            };
        }
        PersistNotes();
        return _notes[subtaskId];
    }

    public void DeleteNote(int subtaskId)
    {
        _notes.Remove(subtaskId);
        PersistNotes();
    }

    public string? GetNote(int subtaskId)
    {
        return _notes.TryGetValue(subtaskId, out var note) ? note.Note : null;
    }

    public SubtaskNoteItem? GetNoteItem(int subtaskId)
    {
        return _notes.TryGetValue(subtaskId, out var note)
            ? new SubtaskNoteItem
            {
                Note = note.Note,
                CreatedDate = note.CreatedDate,
                UpdatedDate = note.UpdatedDate
            }
            : null;
    }

    public Dictionary<int, string> GetAllNotes()
    {
        return _notes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Note);
    }

    private Dictionary<int, SubtaskNoteItem> LoadNotes()
    {
        try
        {
            if (File.Exists(_storageFilePath))
            {
                var json = File.ReadAllText(_storageFilePath);
                var rawMap = JsonSerializer.Deserialize<Dictionary<int, JsonElement>>(json);
                if (rawMap != null)
                {
                    var mapped = new Dictionary<int, SubtaskNoteItem>();
                    var now = DateTime.UtcNow;
                    foreach (var kvp in rawMap)
                    {
                        if (kvp.Value.ValueKind == JsonValueKind.String)
                        {
                            mapped[kvp.Key] = new SubtaskNoteItem
                            {
                                Note = kvp.Value.GetString() ?? string.Empty,
                                CreatedDate = now,
                                UpdatedDate = now
                            };
                            continue;
                        }

                        if (kvp.Value.ValueKind == JsonValueKind.Object)
                        {
                            var item = kvp.Value.Deserialize<SubtaskNoteItem>() ?? new SubtaskNoteItem();
                            if (item.CreatedDate == default)
                                item.CreatedDate = now;
                            if (item.UpdatedDate == default)
                                item.UpdatedDate = item.CreatedDate;
                            mapped[kvp.Key] = item;
                        }
                    }
                    return mapped;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load personal notes: {ex.Message}");
        }

        return new Dictionary<int, SubtaskNoteItem>();
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

public class SubtaskNoteItem
{
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}