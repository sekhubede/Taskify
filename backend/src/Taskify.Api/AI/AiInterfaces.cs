namespace Taskify.Api.AI;

public interface IAiProvider
{
    Task<string> GenerateJsonAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
}

public interface IAiAnalysisService
{
    Task<CommentAnalysisResponse> AnalyzeCommentsAsync(CommentAnalysisRequest request, CancellationToken cancellationToken);
}
