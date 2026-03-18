namespace Taskify.Api.AI;

public sealed record CommentAnalysisRequest(
    int AssignmentId,
    string AssignmentTitle,
    List<CommentAnalysisCommentRequest> Comments,
    string Mode = "full",
    bool IncludePersonalNotes = false
);

public sealed record CommentAnalysisCommentRequest(
    int CommentId,
    string Author,
    string Content,
    DateTime CreatedDate,
    string? PersonalNote = null
);

public sealed record CommentAnalysisResponse(
    string Summary,
    List<string> KeyPoints,
    List<CommentActionItem> ActionItems,
    List<string> Risks,
    string? SuggestedReply,
    CommentAnalysisModelInfo ModelInfo,
    List<string> Warnings
);

public sealed record CommentActionItem(
    string Title,
    string? OwnerHint,
    string Priority,
    string Reason
);

public sealed record CommentAnalysisModelInfo(
    string Provider,
    string Model,
    int DurationMs
);
