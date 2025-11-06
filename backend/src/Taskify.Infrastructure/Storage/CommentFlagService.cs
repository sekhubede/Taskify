using Taskify.Infrastructure.Storage;

namespace Taskify.Infrastructure.Storage;

public class CommentFlagService
{
    private readonly CommentFlagStore _flagStore;

    public CommentFlagService(CommentFlagStore flagStore)
    {
        _flagStore = flagStore;
    }

    public bool IsCommentFlagged(int commentId)
    {
        return _flagStore.IsFlagged(commentId);
    }

    public void SetCommentFlag(int commentId, bool isFlagged)
    {
        _flagStore.SetFlag(commentId, isFlagged);
    }
}

