using System.Text.Json;
using Taskify.Domain.Entities;

namespace Taskify.Infrastructure.Storage;

public class SubtaskStore
{
    private readonly string _storageFilePath;
    private readonly object _syncRoot = new();

    private SubtaskStoreModel _model;

    public SubtaskStore(string storageDirectory = "storage")
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

        _storageFilePath = Path.Combine(storageDirectory, "subtasks.json");
        _model = Load();
    }

    public List<Subtask> GetSubtasksForAssignment(int assignmentId, Func<int, string?> getNote)
    {
        lock (_syncRoot)
        {
            if (!_model.AssignmentIdToSubtasks.TryGetValue(assignmentId, out var items))
                return new List<Subtask>();

            return items
                .OrderBy(i => i.Order)
                .Select(i => new Subtask(
                    id: i.Id,
                    title: i.Title,
                    isCompleted: i.IsCompleted,
                    assignmentId: assignmentId,
                    order: i.Order,
                    createdDate: i.CreatedDate,
                    completedDate: i.CompletedDate,
                    personalNote: getNote(i.Id)))
                .ToList();
        }
    }

    public Subtask AddSubtask(int assignmentId, string title, int? order, Func<int, string?> getNote)
    {
        lock (_syncRoot)
        {
            var id = _model.NextId++;
            var now = DateTime.UtcNow;
            var list = _model.AssignmentIdToSubtasks.GetValueOrDefault(assignmentId) ?? new List<SubtaskItem>();

            var newOrder = order ?? (list.Count == 0 ? 0 : list.Max(i => i.Order) + 1);

            var item = new SubtaskItem
            {
                Id = id,
                Title = title,
                IsCompleted = false,
                Order = newOrder,
                CreatedDate = now,
                CompletedDate = null
            };

            if (!_model.AssignmentIdToSubtasks.ContainsKey(assignmentId))
                _model.AssignmentIdToSubtasks[assignmentId] = list;

            list.Add(item);
            Persist();

            return new Subtask(
                id: id,
                title: title,
                isCompleted: false,
                assignmentId: assignmentId,
                order: newOrder,
                createdDate: now,
                completedDate: null,
                personalNote: getNote(id));
        }
    }

    public bool SetCompletion(int subtaskId, bool isCompleted)
    {
        lock (_syncRoot)
        {
            foreach (var kvp in _model.AssignmentIdToSubtasks)
            {
                var item = kvp.Value.FirstOrDefault(i => i.Id == subtaskId);
                if (item != null)
                {
                    item.IsCompleted = isCompleted;
                    item.CompletedDate = isCompleted ? DateTime.UtcNow : null;
                    Persist();
                    return true;
                }
            }
            return false;
        }
    }

    public bool UpdateOrder(int subtaskId, int newOrder)
    {
        lock (_syncRoot)
        {
            foreach (var kvp in _model.AssignmentIdToSubtasks)
            {
                var item = kvp.Value.FirstOrDefault(i => i.Id == subtaskId);
                if (item != null)
                {
                    item.Order = newOrder;
                    Persist();
                    return true;
                }
            }
            return false;
        }
    }

    public bool ReorderSubtasks(int assignmentId, Dictionary<int, int> subtaskIdToOrder)
    {
        lock (_syncRoot)
        {
            if (!_model.AssignmentIdToSubtasks.TryGetValue(assignmentId, out var items))
                return false;

            foreach (var kvp in subtaskIdToOrder)
            {
                var item = items.FirstOrDefault(i => i.Id == kvp.Key);
                if (item != null)
                {
                    item.Order = kvp.Value;
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
            foreach (var kvp in _model.AssignmentIdToSubtasks)
            {
                var item = kvp.Value.FirstOrDefault(i => i.Id == subtaskId);
                if (item != null)
                {
                    kvp.Value.Remove(item);
                    Persist();
                    return true;
                }
            }
            return false;
        }
    }

    private SubtaskStoreModel Load()
    {
        try
        {
            if (File.Exists(_storageFilePath))
            {
                var json = File.ReadAllText(_storageFilePath);
                var loaded = JsonSerializer.Deserialize<SubtaskStoreModel>(json);
                if (loaded != null)
                {
                    // Ensure non-null collections
                    loaded.AssignmentIdToSubtasks ??= new Dictionary<int, List<SubtaskItem>>();
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load subtasks: {ex.Message}");
        }

        return new SubtaskStoreModel
        {
            NextId = 1,
            AssignmentIdToSubtasks = new Dictionary<int, List<SubtaskItem>>()
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
            Console.WriteLine($"Warning: Failed to persist subtasks: {ex.Message}");
        }
    }

    private class SubtaskStoreModel
    {
        public int NextId { get; set; }
        public Dictionary<int, List<SubtaskItem>> AssignmentIdToSubtasks { get; set; } = new();
    }

    private class SubtaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public int Order { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}


