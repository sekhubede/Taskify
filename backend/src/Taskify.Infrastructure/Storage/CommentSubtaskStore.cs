using System.Text.Json;

namespace Taskify.Infrastructure.Storage;

public class CommentSubtaskStore
{
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

    public List<CommentSubtaskItem> GetSubtasksForComment(int commentId)
    {
        lock (_syncRoot)
        {
            if (!_model.CommentIdToSubtasks.TryGetValue(commentId, out var items))
                return new List<CommentSubtaskItem>();

            return items
                .OrderBy(i => i.Order)
                .Select(i => new CommentSubtaskItem
                {
                    Id = i.Id,
                    CommentId = commentId,
                    Title = i.Title,
                    IsCompleted = i.IsCompleted,
                    Order = i.Order,
                    CreatedDate = i.CreatedDate,
                    CompletedDate = i.CompletedDate
                })
                .ToList();
        }
    }

    public CommentSubtaskItem AddSubtask(int commentId, string title, int? order = null)
    {
        lock (_syncRoot)
        {
            var id = _model.NextId++;
            var now = DateTime.UtcNow;
            var list = _model.CommentIdToSubtasks.GetValueOrDefault(commentId) ?? new List<CommentSubtaskItemModel>();
            var newOrder = order ?? (list.Count == 0 ? 0 : list.Max(i => i.Order) + 1);

            var item = new CommentSubtaskItemModel
            {
                Id = id,
                Title = title,
                IsCompleted = false,
                Order = newOrder,
                CreatedDate = now
            };

            if (!_model.CommentIdToSubtasks.ContainsKey(commentId))
                _model.CommentIdToSubtasks[commentId] = list;

            list.Add(item);
            Persist();

            return new CommentSubtaskItem
            {
                Id = id,
                CommentId = commentId,
                Title = title,
                IsCompleted = false,
                Order = newOrder,
                CreatedDate = now
            };
        }
    }

    public bool SetCompletion(int subtaskId, bool isCompleted)
    {
        lock (_syncRoot)
        {
            foreach (var kvp in _model.CommentIdToSubtasks)
            {
                var item = kvp.Value.FirstOrDefault(i => i.Id == subtaskId);
                if (item == null)
                    continue;

                item.IsCompleted = isCompleted;
                item.CompletedDate = isCompleted ? DateTime.UtcNow : null;
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
            foreach (var kvp in _model.CommentIdToSubtasks)
            {
                var item = kvp.Value.FirstOrDefault(i => i.Id == subtaskId);
                if (item == null)
                    continue;

                item.Title = title;
                Persist();
                return true;
            }

            return false;
        }
    }

    public bool ReorderSubtasks(int commentId, Dictionary<int, int> subtaskIdToOrder)
    {
        lock (_syncRoot)
        {
            if (!_model.CommentIdToSubtasks.TryGetValue(commentId, out var items))
                return false;

            foreach (var kvp in subtaskIdToOrder)
            {
                var item = items.FirstOrDefault(i => i.Id == kvp.Key);
                if (item != null)
                    item.Order = kvp.Value;
            }

            Persist();
            return true;
        }
    }

    public bool DeleteSubtask(int subtaskId)
    {
        lock (_syncRoot)
        {
            foreach (var kvp in _model.CommentIdToSubtasks)
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
                var loaded = JsonSerializer.Deserialize<CommentSubtaskStoreModel>(json);
                if (loaded != null)
                {
                    loaded.CommentIdToSubtasks ??= new Dictionary<int, List<CommentSubtaskItemModel>>();
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load comment subtasks: {ex.Message}");
        }

        return new CommentSubtaskStoreModel
        {
            NextId = 1,
            CommentIdToSubtasks = new Dictionary<int, List<CommentSubtaskItemModel>>()
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
        public Dictionary<int, List<CommentSubtaskItemModel>> CommentIdToSubtasks { get; set; } = new();
    }

    private class CommentSubtaskItemModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public int Order { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}

public class CommentSubtaskItem
{
    public int Id { get; set; }
    public int CommentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int Order { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
}
