namespace Taskify.Api.AI;

public sealed record CommentAnalysisRequest(
    int AssignmentId,
    string AssignmentTitle,
    string? AssignmentDescription,
    List<CommentAnalysisCommentRequest> Comments,
    List<CommentAnalysisSubtaskRequest>? ExistingSubtasks = null,
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

public sealed record CommentAnalysisSubtaskRequest(
    int? SubtaskId,
    string Title,
    bool IsCompleted
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

public sealed record AiGenerationOptions(
    int NumPredict = 420,
    double Temperature = 0.2,
    double TopP = 0.9
);
