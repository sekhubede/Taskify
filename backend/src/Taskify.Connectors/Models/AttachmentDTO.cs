namespace Taskify.Connectors;

/// <summary>
/// Normalized attachment metadata from the source system.
/// </summary>
public class AttachmentDTO
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
}

/// <summary>
/// Attachment payload used when downloading attachment content.
/// </summary>
public class AttachmentFileDTO
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
