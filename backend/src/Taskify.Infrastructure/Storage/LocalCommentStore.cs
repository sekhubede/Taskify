using System.Text.Json;

namespace Taskify.Infrastructure.Storage;

public class LocalCommentStore
{
    private readonly string _storageFilePath;
    private readonly object _syncRoot = new();
    private CommentStoreModel _model;

    public LocalCommentStore(string storageDirectory = "storage")
    {
        if (!Path.IsPathRooted(storageDirectory))
        {
            storageDirectory = Path.Combine(AppContext.BaseDirectory, storageDirectory);
        }

        if (!Directory.Exists(storageDirectory))
        {
            Directory.CreateDirectory(storageDirectory);
        }

        _storageFilePath = Path.Combine(storageDirectory, "comments.json");
        _model = Load();
    }

    public List<CommentItem> GetCommentsForAssignment(int assignmentId)
    {
        lock (_syncRoot)
        {
            if (!_model.AssignmentIdToComments.TryGetValue(assignmentId, out var comments))
                return new List<CommentItem>();

            return comments.OrderBy(c => c.CreatedDate).ToList();
        }
    }

    public CommentItem AddComment(int assignmentId, string content, string authorName)
    {
        lock (_syncRoot)
        {
            var id = _model.NextId++;
            var now = DateTime.UtcNow;

            var item = new CommentItem
            {
                Id = id,
                Content = content,
                AuthorName = authorName,
                CreatedDate = now
            };

            if (!_model.AssignmentIdToComments.ContainsKey(assignmentId))
                _model.AssignmentIdToComments[assignmentId] = new List<CommentItem>();

            _model.AssignmentIdToComments[assignmentId].Add(item);
            Persist();

            return item;
        }
    }

    public int GetCommentCount(int assignmentId)
    {
        lock (_syncRoot)
        {
            if (!_model.AssignmentIdToComments.TryGetValue(assignmentId, out var comments))
                return 0;

            return comments.Count;
        }
    }

    private CommentStoreModel Load()
    {
        try
        {
            if (File.Exists(_storageFilePath))
            {
                var json = File.ReadAllText(_storageFilePath);
                var loaded = JsonSerializer.Deserialize<CommentStoreModel>(json);
                if (loaded != null)
                {
                    loaded.AssignmentIdToComments ??= new Dictionary<int, List<CommentItem>>();
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load comments: {ex.Message}");
        }

        return new CommentStoreModel
        {
            NextId = 1,
            AssignmentIdToComments = new Dictionary<int, List<CommentItem>>()
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
            Console.WriteLine($"Warning: Failed to persist comments: {ex.Message}");
        }
    }

    public class CommentStoreModel
    {
        public int NextId { get; set; }
        public Dictionary<int, List<CommentItem>> AssignmentIdToComments { get; set; } = new();
    }

    public class CommentItem
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
