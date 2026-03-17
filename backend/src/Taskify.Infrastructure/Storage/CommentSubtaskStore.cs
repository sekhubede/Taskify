using System.Text.Json;

namespace Taskify.Infrastructure.Storage;

public class CommentSubtaskStore
{
    private const string LegacyScopePrefix = "legacy:";
    private readonly string _storageFilePath;
    private readonly object _syncRoot = new();
    private CommentSubtaskStoreModel _model;

    public CommentSubtaskStore(string storageDirectory = "storage")
    {
        if (!Path.IsPathRooted(storageDirectory))
            storageDirectory = Path.Combine(AppContext.BaseDirectory, storageDirectory);

        if (!Directory.Exists(storageDirectory))
            Directory.CreateDirectory(storageDirectory);

        _storageFilePath = Path.Combine(storageDirectory, "comment_subtasks.json");
        _model = Load();
    }

    public List<CommentSubtaskItem> GetSubtasksForComment(int assignmentId, int commentId)
    {
        lock (_syncRoot)
        {
            var scopeKey = BuildScopeKey(assignmentId, commentId);
            if (!_model.CommentScopeToSubtasks.TryGetValue(scopeKey, out var items))
            {
                var legacyScopeKey = BuildLegacyScopeKey(commentId);
                _model.CommentScopeToSubtasks.TryGetValue(legacyScopeKey, out items);
            }

            if (items == null)
                return new List<CommentSubtaskItem>();

            return items
                .OrderBy(i => i.Order)
                .Select(i => new CommentSubtaskItem
                {
                    Id = i.Id,
                    AssignmentId = assignmentId,
                    CommentId = commentId,
                    Title = i.Title,
                    IsCompleted = i.IsCompleted,
                    Order = i.Order,
                    CreatedDate = i.CreatedDate,
                    CompletedDate = i.CompletedDate,
                    UpdatedDate = i.UpdatedDate
                })
                .ToList();
        }
    }

    public CommentSubtaskItem AddSubtask(int assignmentId, int commentId, string title, int? order = null)
    {
        lock (_syncRoot)
        {
            var id = _model.NextId++;
            var now = DateTime.UtcNow;
            var scopeKey = BuildScopeKey(assignmentId, commentId);
            var legacyScopeKey = BuildLegacyScopeKey(commentId);
            var list = _model.CommentScopeToSubtasks.GetValueOrDefault(scopeKey)
                ?? _model.CommentScopeToSubtasks.GetValueOrDefault(legacyScopeKey)
                ?? new List<CommentSubtaskItemModel>();
            var newOrder = order ?? (list.Count == 0 ? 0 : list.Max(i => i.Order) + 1);

            var item = new CommentSubtaskItemModel
            {
                Id = id,
                Title = title,
                IsCompleted = false,
                Order = newOrder,
                CreatedDate = now,
                UpdatedDate = now
            };

            if (!_model.CommentScopeToSubtasks.ContainsKey(scopeKey))
                _model.CommentScopeToSubtasks[scopeKey] = list;
            if (_model.CommentScopeToSubtasks.ContainsKey(legacyScopeKey))
                _model.CommentScopeToSubtasks.Remove(legacyScopeKey);

            list.Add(item);
            Persist();

            return new CommentSubtaskItem
            {
                Id = id,
                AssignmentId = assignmentId,
                CommentId = commentId,
                Title = title,
                IsCompleted = false,
                Order = newOrder,
                CreatedDate = now,
                UpdatedDate = now
            };
        }
    }

    public bool SetCompletion(int subtaskId, bool isCompleted)
    {
        lock (_syncRoot)
        {
            foreach (var kvp in _model.CommentScopeToSubtasks)
            {
                var item = kvp.Value.FirstOrDefault(i => i.Id == subtaskId);
                if (item == null)
                    continue;

                item.IsCompleted = isCompleted;
                item.CompletedDate = isCompleted ? DateTime.UtcNow : null;
                item.UpdatedDate = DateTime.UtcNow;
                Persist();
                return true;
            }

            return false;
        }
    }

    public bool SetTitle(int subtaskId, string title)
    {
        lock (_syncRoot)
        {
            foreach (var kvp in _model.CommentScopeToSubtasks)
            {
                var item = kvp.Value.FirstOrDefault(i => i.Id == subtaskId);
                if (item == null)
                    continue;

                item.Title = title;
                item.UpdatedDate = DateTime.UtcNow;
                Persist();
                return true;
            }

            return false;
        }
    }

    public bool ReorderSubtasks(int assignmentId, int commentId, Dictionary<int, int> subtaskIdToOrder)
    {
        lock (_syncRoot)
        {
            var scopeKey = BuildScopeKey(assignmentId, commentId);
            if (!_model.CommentScopeToSubtasks.TryGetValue(scopeKey, out var items))
            {
                var legacyScopeKey = BuildLegacyScopeKey(commentId);
                if (_model.CommentScopeToSubtasks.TryGetValue(legacyScopeKey, out var legacyItems))
                {
                    items = legacyItems;
                    _model.CommentScopeToSubtasks[scopeKey] = items;
                    _model.CommentScopeToSubtasks.Remove(legacyScopeKey);
                }
            }

            if (items == null)
                return false;

            foreach (var kvp in subtaskIdToOrder)
            {
                var item = items.FirstOrDefault(i => i.Id == kvp.Key);
                if (item != null)
                {
                    item.Order = kvp.Value;
                    item.UpdatedDate = DateTime.UtcNow;
                }
            }

            Persist();
            return true;
        }
    }

    public bool DeleteSubtask(int subtaskId)
    {
        lock (_syncRoot)
        {
            foreach (var kvp in _model.CommentScopeToSubtasks)
            {
                var item = kvp.Value.FirstOrDefault(i => i.Id == subtaskId);
                if (item == null)
                    continue;

                kvp.Value.Remove(item);
                Persist();
                return true;
            }

            return false;
        }
    }

    private CommentSubtaskStoreModel Load()
    {
        try
        {
            if (File.Exists(_storageFilePath))
            {
                var json = File.ReadAllText(_storageFilePath);
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                var loaded = new CommentSubtaskStoreModel();
                if (root.TryGetProperty("NextId", out var nextIdElement) &&
                    nextIdElement.ValueKind == JsonValueKind.Number)
                {
                    loaded.NextId = nextIdElement.GetInt32();
                }

                if (root.TryGetProperty("CommentScopeToSubtasks", out var scopedElement) &&
                    scopedElement.ValueKind == JsonValueKind.Object)
                {
                    loaded.CommentScopeToSubtasks = DeserializeScopedMap(scopedElement);
                }
                else if (root.TryGetProperty("CommentIdToSubtasks", out var legacyElement) &&
                    legacyElement.ValueKind == JsonValueKind.Object)
                {
                    loaded.CommentScopeToSubtasks = DeserializeLegacyMap(legacyElement);
                }

                foreach (var items in loaded.CommentScopeToSubtasks.Values)
                {
                    foreach (var item in items)
                    {
                        if (item.CreatedDate == default)
                            item.CreatedDate = DateTime.UtcNow;
                        if (item.UpdatedDate == default)
                            item.UpdatedDate = item.CreatedDate;
                    }
                }

                return loaded;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load comment subtasks: {ex.Message}");
        }

        return new CommentSubtaskStoreModel
        {
            NextId = 1,
            CommentScopeToSubtasks = new Dictionary<string, List<CommentSubtaskItemModel>>()
        };
    }

    private void Persist()
    {
        try
        {
            var json = JsonSerializer.Serialize(_model, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_storageFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to persist comment subtasks: {ex.Message}");
        }
    }

    private class CommentSubtaskStoreModel
    {
        public int NextId { get; set; }
        public Dictionary<string, List<CommentSubtaskItemModel>> CommentScopeToSubtasks { get; set; } = new();
    }

    private class CommentSubtaskItemModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public int Order { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    private static string BuildScopeKey(int assignmentId, int commentId) =>
        $"{assignmentId}:{commentId}";

    private static string BuildLegacyScopeKey(int commentId) =>
        $"{LegacyScopePrefix}{commentId}";

    private static Dictionary<string, List<CommentSubtaskItemModel>> DeserializeScopedMap(JsonElement scopedElement)
    {
        var result = new Dictionary<string, List<CommentSubtaskItemModel>>();
        foreach (var property in scopedElement.EnumerateObject())
        {
            var list = property.Value.Deserialize<List<CommentSubtaskItemModel>>() ?? new List<CommentSubtaskItemModel>();
            result[property.Name] = list;
        }
        return result;
    }

    private static Dictionary<string, List<CommentSubtaskItemModel>> DeserializeLegacyMap(JsonElement legacyElement)
    {
        var result = new Dictionary<string, List<CommentSubtaskItemModel>>();
        foreach (var property in legacyElement.EnumerateObject())
        {
            if (!int.TryParse(property.Name, out var commentId))
                continue;

            var list = property.Value.Deserialize<List<CommentSubtaskItemModel>>() ?? new List<CommentSubtaskItemModel>();
            result[BuildLegacyScopeKey(commentId)] = list;
        }
        return result;
    }
}

public class CommentSubtaskItem
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public int CommentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int Order { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
