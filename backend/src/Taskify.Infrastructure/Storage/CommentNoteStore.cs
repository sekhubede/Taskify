using System.Text.Json;

namespace Taskify.Infrastructure.Storage;

public class CommentNoteStore
{
    private readonly string _storageFilePath;
    private Dictionary<int, CommentNoteItem> _notes;

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

    public CommentNoteItem SaveNote(int commentId, string note)
    {
        var now = DateTime.UtcNow;
        if (_notes.TryGetValue(commentId, out var existing))
        {
            existing.Note = note;
            existing.UpdatedDate = now;
            _notes[commentId] = existing;
        }
        else
        {
            _notes[commentId] = new CommentNoteItem
            {
                Note = note,
                CreatedDate = now,
                UpdatedDate = now
            };
        }
        PersistNotes();
        return _notes[commentId];
    }

    public void DeleteNote(int commentId)
    {
        _notes.Remove(commentId);
        PersistNotes();
    }

    public string? GetNote(int commentId)
    {
        return _notes.TryGetValue(commentId, out var note) ? note.Note : null;
    }

    public CommentNoteItem? GetNoteItem(int commentId)
    {
        return _notes.TryGetValue(commentId, out var note)
            ? new CommentNoteItem
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

    private Dictionary<int, CommentNoteItem> LoadNotes()
    {
        try
        {
            if (File.Exists(_storageFilePath))
            {
                var json = File.ReadAllText(_storageFilePath);
                var rawMap = JsonSerializer.Deserialize<Dictionary<int, JsonElement>>(json);
                if (rawMap != null)
                {
                    var mapped = new Dictionary<int, CommentNoteItem>();
                    var now = DateTime.UtcNow;
                    foreach (var kvp in rawMap)
                    {
                        if (kvp.Value.ValueKind == JsonValueKind.String)
                        {
                            mapped[kvp.Key] = new CommentNoteItem
                            {
                                Note = kvp.Value.GetString() ?? string.Empty,
                                CreatedDate = now,
                                UpdatedDate = now
                            };
                            continue;
                        }

                        if (kvp.Value.ValueKind == JsonValueKind.Object)
                        {
                            var item = kvp.Value.Deserialize<CommentNoteItem>() ?? new CommentNoteItem();
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
            Console.WriteLine($"Warning: Failed to load comment notes: {ex.Message}");
        }

        return new Dictionary<int, CommentNoteItem>();
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

public class CommentNoteItem
{
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

