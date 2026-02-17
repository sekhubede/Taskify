namespace Taskify.Connectors;

/// <summary>
/// Normalized comment from the source system.
/// Every connector maps its comment format into this DTO.
/// </summary>
public class CommentDTO
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public int AssignmentId { get; set; }
}
