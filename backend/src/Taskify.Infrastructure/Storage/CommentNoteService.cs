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

    public void UpdateCommentNote(int commentId, string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            _noteStore.DeleteNote(commentId);
        }
        else
        {
            if (note.Length > 1000)
                throw new ArgumentException("Comment note cannot exceed 1000 characters");
            
            _noteStore.SaveNote(commentId, note);
        }
    }
}

