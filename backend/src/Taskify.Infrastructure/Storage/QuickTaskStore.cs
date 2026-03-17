using System.Text.Json;

namespace Taskify.Infrastructure.Storage;

public class QuickTaskStore
{
    private readonly string _storageFilePath;
    private readonly object _syncRoot = new();
    private QuickTaskStoreModel _model;

    public QuickTaskStore(string storageDirectory = "storage")
    {
        if (!Path.IsPathRooted(storageDirectory))
            storageDirectory = Path.Combine(AppContext.BaseDirectory, storageDirectory);

        if (!Directory.Exists(storageDirectory))
            Directory.CreateDirectory(storageDirectory);

        _storageFilePath = Path.Combine(storageDirectory, "quick_tasks.json");
        _model = Load();
    }

    public List<QuickTaskItem> GetTasks()
    {
        lock (_syncRoot)
        {
            return _model.Tasks
                .Select(CloneTask)
                .OrderBy(t => t.IsCompleted)
                .ThenByDescending(t => t.CreatedDate)
                .ToList();
        }
    }

    public QuickTaskItem AddTask(string title)
    {
        lock (_syncRoot)
        {
            var now = DateTime.UtcNow;
            var task = new QuickTaskModel
            {
                Id = _model.NextTaskId++,
                Title = title,
                IsCompleted = false,
                CreatedDate = now
            };
            _model.Tasks.Add(task);
            Persist();
            return CloneTask(task);
        }
    }

    public bool UpdateTaskTitle(int taskId, string title)
    {
        lock (_syncRoot)
        {
            var task = _model.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
                return false;

            task.Title = title;
            Persist();
            return true;
        }
    }

    public bool SetTaskCompletion(int taskId, bool isCompleted)
    {
        lock (_syncRoot)
        {
            var task = _model.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
                return false;

            task.IsCompleted = isCompleted;
            task.CompletedDate = isCompleted ? DateTime.UtcNow : null;
            Persist();
            return true;
        }
    }

    public bool DeleteTask(int taskId)
    {
        lock (_syncRoot)
        {
            var task = _model.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
                return false;

            _model.Tasks.Remove(task);
            Persist();
            return true;
        }
    }

    public List<QuickTaskCommentItem> GetTaskComments(int taskId)
    {
        lock (_syncRoot)
        {
            var task = _model.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
                return new List<QuickTaskCommentItem>();

            return task.Comments
                .OrderBy(c => c.CreatedDate)
                .Select(c => new QuickTaskCommentItem
                {
                    Id = c.Id,
                    TaskId = taskId,
                    Content = c.Content,
                    CreatedDate = c.CreatedDate
                })
                .ToList();
        }
    }

    public QuickTaskCommentItem AddTaskComment(int taskId, string content)
    {
        lock (_syncRoot)
        {
            var task = _model.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
                throw new ArgumentException("Task not found", nameof(taskId));

            var comment = new QuickTaskCommentModel
            {
                Id = _model.NextCommentId++,
                Content = content,
                CreatedDate = DateTime.UtcNow
            };
            task.Comments.Add(comment);
            Persist();

            return new QuickTaskCommentItem
            {
                Id = comment.Id,
                TaskId = taskId,
                Content = comment.Content,
                CreatedDate = comment.CreatedDate
            };
        }
    }

    public List<QuickTaskChecklistItem> GetTaskChecklist(int taskId)
    {
        lock (_syncRoot)
        {
            var task = _model.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
                return new List<QuickTaskChecklistItem>();

            return task.Checklist
                .OrderBy(i => i.Order)
                .Select(i => new QuickTaskChecklistItem
                {
                    Id = i.Id,
                    TaskId = taskId,
                    Title = i.Title,
                    IsCompleted = i.IsCompleted,
                    Order = i.Order,
                    CreatedDate = i.CreatedDate,
                    CompletedDate = i.CompletedDate
                })
                .ToList();
        }
    }

    public QuickTaskChecklistItem AddChecklistItem(int taskId, string title, int? order = null)
    {
        lock (_syncRoot)
        {
            var task = _model.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
                throw new ArgumentException("Task not found", nameof(taskId));

            var newOrder = order ?? (task.Checklist.Count == 0 ? 0 : task.Checklist.Max(i => i.Order) + 1);
            var item = new QuickTaskChecklistItemModel
            {
                Id = _model.NextChecklistId++,
                Title = title,
                IsCompleted = false,
                Order = newOrder,
                CreatedDate = DateTime.UtcNow
            };

            task.Checklist.Add(item);
            Persist();

            return new QuickTaskChecklistItem
            {
                Id = item.Id,
                TaskId = taskId,
                Title = item.Title,
                IsCompleted = item.IsCompleted,
                Order = item.Order,
                CreatedDate = item.CreatedDate
            };
        }
    }

    public bool SetChecklistCompletion(int checklistItemId, bool isCompleted)
    {
        lock (_syncRoot)
        {
            foreach (var task in _model.Tasks)
            {
                var item = task.Checklist.FirstOrDefault(i => i.Id == checklistItemId);
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

    public bool SetChecklistTitle(int checklistItemId, string title)
    {
        lock (_syncRoot)
        {
            foreach (var task in _model.Tasks)
            {
                var item = task.Checklist.FirstOrDefault(i => i.Id == checklistItemId);
                if (item == null)
                    continue;

                item.Title = title;
                Persist();
                return true;
            }

            return false;
        }
    }

    public bool ReorderChecklist(int taskId, Dictionary<int, int> checklistItemToOrder)
    {
        lock (_syncRoot)
        {
            var task = _model.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
                return false;

            foreach (var kvp in checklistItemToOrder)
            {
                var item = task.Checklist.FirstOrDefault(i => i.Id == kvp.Key);
                if (item != null)
                    item.Order = kvp.Value;
            }

            Persist();
            return true;
        }
    }

    public bool DeleteChecklistItem(int checklistItemId)
    {
        lock (_syncRoot)
        {
            foreach (var task in _model.Tasks)
            {
                var item = task.Checklist.FirstOrDefault(i => i.Id == checklistItemId);
                if (item == null)
                    continue;

                task.Checklist.Remove(item);
                Persist();
                return true;
            }

            return false;
        }
    }

    private static QuickTaskItem CloneTask(QuickTaskModel model)
    {
        return new QuickTaskItem
        {
            Id = model.Id,
            Title = model.Title,
            IsCompleted = model.IsCompleted,
            CreatedDate = model.CreatedDate,
            CompletedDate = model.CompletedDate,
            Comments = model.Comments
                .OrderBy(c => c.CreatedDate)
                .Select(c => new QuickTaskCommentItem
                {
                    Id = c.Id,
                    TaskId = model.Id,
                    Content = c.Content,
                    CreatedDate = c.CreatedDate
                })
                .ToList(),
            Checklist = model.Checklist
                .OrderBy(i => i.Order)
                .Select(i => new QuickTaskChecklistItem
                {
                    Id = i.Id,
                    TaskId = model.Id,
                    Title = i.Title,
                    IsCompleted = i.IsCompleted,
                    Order = i.Order,
                    CreatedDate = i.CreatedDate,
                    CompletedDate = i.CompletedDate
                })
                .ToList()
        };
    }

    private QuickTaskStoreModel Load()
    {
        try
        {
            if (File.Exists(_storageFilePath))
            {
                var json = File.ReadAllText(_storageFilePath);
                var loaded = JsonSerializer.Deserialize<QuickTaskStoreModel>(json);
                if (loaded != null)
                {
                    loaded.Tasks ??= new List<QuickTaskModel>();
                    foreach (var task in loaded.Tasks)
                    {
                        task.Comments ??= new List<QuickTaskCommentModel>();
                        task.Checklist ??= new List<QuickTaskChecklistItemModel>();
                    }
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load quick tasks: {ex.Message}");
        }

        return new QuickTaskStoreModel
        {
            NextTaskId = 1,
            NextCommentId = 1,
            NextChecklistId = 1,
            Tasks = new List<QuickTaskModel>()
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
            Console.WriteLine($"Warning: Failed to persist quick tasks: {ex.Message}");
        }
    }

    private class QuickTaskStoreModel
    {
        public int NextTaskId { get; set; }
        public int NextCommentId { get; set; }
        public int NextChecklistId { get; set; }
        public List<QuickTaskModel> Tasks { get; set; } = new();
    }

    private class QuickTaskModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public List<QuickTaskCommentModel> Comments { get; set; } = new();
        public List<QuickTaskChecklistItemModel> Checklist { get; set; } = new();
    }

    private class QuickTaskCommentModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    private class QuickTaskChecklistItemModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public int Order { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}

public class QuickTaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public List<QuickTaskCommentItem> Comments { get; set; } = new();
    public List<QuickTaskChecklistItem> Checklist { get; set; } = new();
}

public class QuickTaskCommentItem
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public class QuickTaskChecklistItem
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int Order { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
}
