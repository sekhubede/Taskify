using Taskify.Infrastructure.Storage;

namespace Taskify.Infrastructure.Storage;

public class CommentNoteService
{
    private readonly CommentNoteStore _noteStore;

    public CommentNoteService(CommentNoteStore noteStore)
    {
        _noteStore = noteStore;
    }

    public string? GetCommentNote(int commentId)
    {
        return _noteStore.GetNote(commentId);
    }

    public CommentNoteItem? GetCommentNoteItem(int commentId)
    {
        return _noteStore.GetNoteItem(commentId);
    }

    public CommentNoteItem? UpdateCommentNote(int commentId, string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            _noteStore.DeleteNote(commentId);
            return null;
        }
        else
        {
            if (note.Length > 1000)
                throw new ArgumentException("Comment note cannot exceed 1000 characters");
            
            return _noteStore.SaveNote(commentId, note);
        }
    }
}

