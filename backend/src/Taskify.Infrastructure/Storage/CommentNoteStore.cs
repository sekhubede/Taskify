using System.Text.Json;

namespace Taskify.Infrastructure.Storage;

public class CommentNoteStore
{
    private readonly string _storageFilePath;
    private Dictionary<string, CommentNoteItem> _notes;

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

    public CommentNoteItem SaveNote(int assignmentId, int commentId, string note)
    {
        var scopedKey = BuildScopedKey(assignmentId, commentId);
        var legacyKey = BuildLegacyKey(commentId);
        var now = DateTime.UtcNow;
        if (_notes.TryGetValue(scopedKey, out var existing))
        {
            existing.Note = note;
            existing.UpdatedDate = now;
            _notes[scopedKey] = existing;
        }
        else
        {
            _notes[scopedKey] = new CommentNoteItem
            {
                Note = note,
                CreatedDate = now,
                UpdatedDate = now
            };
        }

        // Remove legacy key once a scoped note is saved.
        _notes.Remove(legacyKey);
        PersistNotes();
        return _notes[scopedKey];
    }

    public void DeleteNote(int assignmentId, int commentId)
    {
        _notes.Remove(BuildScopedKey(assignmentId, commentId));
        _notes.Remove(BuildLegacyKey(commentId));
        PersistNotes();
    }

    public string? GetNote(int assignmentId, int commentId)
    {
        return GetNoteItem(assignmentId, commentId)?.Note;
    }

    public CommentNoteItem? GetNoteItem(int assignmentId, int commentId)
    {
        var scopedKey = BuildScopedKey(assignmentId, commentId);
        if (!_notes.TryGetValue(scopedKey, out var note))
        {
            // One-time migration path for legacy storage keyed only by commentId.
            var legacyKey = BuildLegacyKey(commentId);
            if (_notes.TryGetValue(legacyKey, out var legacy))
            {
                _notes[scopedKey] = legacy;
                _notes.Remove(legacyKey);
                PersistNotes();
                note = legacy;
            }
        }

        return note != null
            ? new CommentNoteItem
            {
                Note = note.Note,
                CreatedDate = note.CreatedDate,
                UpdatedDate = note.UpdatedDate
            }
            : null;
    }

    public Dictionary<string, string> GetAllNotes()
    {
        return _notes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Note);
    }

    private Dictionary<string, CommentNoteItem> LoadNotes()
    {
        try
        {
            if (File.Exists(_storageFilePath))
            {
                var json = File.ReadAllText(_storageFilePath);
                var rawMap = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (rawMap != null)
                {
                    var mapped = new Dictionary<string, CommentNoteItem>();
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

        return new Dictionary<string, CommentNoteItem>();
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

    private static string BuildScopedKey(int assignmentId, int commentId)
    {
        return $"{assignmentId}:{commentId}";
    }

    private static string BuildLegacyKey(int commentId)
    {
        return commentId.ToString();
    }
}

public class CommentNoteItem
{
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

