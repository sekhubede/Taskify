using Taskify.Infrastructure.Storage;

namespace Taskify.Infrastructure.Storage;

public class CommentNoteService
{
    private readonly CommentNoteStore _noteStore;

    public CommentNoteService(CommentNoteStore noteStore)
    {
        _noteStore = noteStore;
    }

    public string? GetCommentNote(int assignmentId, int commentId)
    {
        return _noteStore.GetNote(assignmentId, commentId);
    }

    public CommentNoteItem? GetCommentNoteItem(int assignmentId, int commentId)
    {
        return _noteStore.GetNoteItem(assignmentId, commentId);
    }

    public CommentNoteItem? UpdateCommentNote(int assignmentId, int commentId, string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            _noteStore.DeleteNote(assignmentId, commentId);
            return null;
        }
        else
        {
            if (note.Length > 1000)
                throw new ArgumentException("Comment note cannot exceed 1000 characters");
            
            return _noteStore.SaveNote(assignmentId, commentId, note);
        }
    }
}

