using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Taskify.Api.AI;

public sealed class AiAnalysisService : IAiAnalysisService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IAiProvider _provider;
    private readonly TaskifyAiOptions _options;

    public AiAnalysisService(IAiProvider provider, IOptions<TaskifyAiOptions> options)
    {
        _provider = provider;
        _options = options.Value;
    }

    public async Task<CommentAnalysisResponse> AnalyzeCommentsAsync(
        CommentAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request);

        var normalizedMode = NormalizeMode(request.Mode);
        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(request, normalizedMode);

        var timer = Stopwatch.StartNew();
        var raw = await _provider.GenerateJsonAsync(systemPrompt, userPrompt, cancellationToken);

        if (!TryParseResponse(raw, out var parsed))
        {
            var retryPrompt = $"{userPrompt}\n\nYour previous response was not valid JSON. Return only valid JSON for the schema.";
            raw = await _provider.GenerateJsonAsync(systemPrompt, retryPrompt, cancellationToken);

            if (!TryParseResponse(raw, out parsed))
            {
                throw new InvalidOperationException("AI response could not be parsed as valid JSON.");
            }
        }

        timer.Stop();
        return NormalizeResponse(parsed!, normalizedMode, timer.ElapsedMilliseconds);
    }

    private static void Validate(CommentAnalysisRequest request)
    {
        if (request.AssignmentId <= 0)
        {
            throw new ArgumentException("AssignmentId must be greater than 0.");
        }

        if (request.Comments == null || request.Comments.Count == 0)
        {
            throw new ArgumentException("At least one comment is required.");
        }
    }

    private static string NormalizeMode(string? mode)
    {
        var normalized = (mode ?? "full").Trim().ToLowerInvariant();
        return normalized is "summary" or "actions" or "full" ? normalized : "full";
    }

    private static string BuildSystemPrompt()
    {
        return """
You are an assistant for analyzing assignment comments.
Return only valid JSON with this exact schema:
{
  "summary": "string",
  "keyPoints": ["string"],
  "actionItems": [
    {
      "title": "string",
      "ownerHint": "string|null",
      "priority": "low|medium|high",
      "reason": "string"
    }
  ],
  "risks": ["string"],
  "suggestedReply": "string|null",
  "warnings": ["string"]
}

Rules:
- Do not include markdown.
- Keep summary concise and factual.
- Do not invent facts not in the provided comments.
- If context is missing, include a warning.
""";
    }

    private static string BuildUserPrompt(CommentAnalysisRequest request, string mode)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Mode: {mode}");
        sb.AppendLine($"Assignment Id: {request.AssignmentId}");
        sb.AppendLine($"Assignment Title: {request.AssignmentTitle}");
        sb.AppendLine($"Include Personal Notes: {request.IncludePersonalNotes}");
        sb.AppendLine("Comments (oldest to newest):");

        foreach (var comment in request.Comments.OrderBy(c => c.CreatedDate))
        {
            sb.AppendLine($"- CommentId: {comment.CommentId}");
            sb.AppendLine($"  Author: {comment.Author}");
            sb.AppendLine($"  Created: {comment.CreatedDate:O}");
            sb.AppendLine($"  Content: {comment.Content}");
            if (request.IncludePersonalNotes && !string.IsNullOrWhiteSpace(comment.PersonalNote))
            {
                sb.AppendLine($"  PersonalNote: {comment.PersonalNote}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Focus by mode:");
        sb.AppendLine("- summary: emphasize summary + keyPoints, keep actionItems short.");
        sb.AppendLine("- actions: emphasize actionItems + risks.");
        sb.AppendLine("- full: provide all sections.");
        return sb.ToString();
    }

    private static bool TryParseResponse(string raw, out RawCommentAnalysisResponse? parsed)
    {
        parsed = null;
        try
        {
            parsed = JsonSerializer.Deserialize<RawCommentAnalysisResponse>(raw, JsonOptions);
            return parsed != null;
        }
        catch
        {
            return false;
        }
    }

    private CommentAnalysisResponse NormalizeResponse(
        RawCommentAnalysisResponse parsed,
        string mode,
        long elapsedMs)
    {
        var keyPoints = (parsed.KeyPoints ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .ToList();

        var risks = (parsed.Risks ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .ToList();

        var warnings = (parsed.Warnings ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .ToList();

        var actionItems = (parsed.ActionItems ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Title))
            .Select(item => new CommentActionItem(
                Title: item.Title.Trim(),
                OwnerHint: string.IsNullOrWhiteSpace(item.OwnerHint) ? null : item.OwnerHint.Trim(),
                Priority: NormalizePriority(item.Priority),
                Reason: string.IsNullOrWhiteSpace(item.Reason) ? "No reason provided." : item.Reason.Trim()
            ))
            .ToList();

        var suggestedReply = string.IsNullOrWhiteSpace(parsed.SuggestedReply)
            ? null
            : parsed.SuggestedReply.Trim();

        if (mode == "summary" && actionItems.Count > 5)
        {
            actionItems = actionItems.Take(5).ToList();
            warnings.Add("Action items were truncated for summary mode.");
        }

        var summary = string.IsNullOrWhiteSpace(parsed.Summary)
            ? "No summary returned by the model."
            : parsed.Summary.Trim();

        return new CommentAnalysisResponse(
            Summary: summary,
            KeyPoints: keyPoints,
            ActionItems: actionItems,
            Risks: risks,
            SuggestedReply: suggestedReply,
            ModelInfo: new CommentAnalysisModelInfo(
                Provider: _options.Provider,
                Model: _options.Model,
                DurationMs: (int)Math.Min(int.MaxValue, elapsedMs)
            ),
            Warnings: warnings
        );
    }

    private static string NormalizePriority(string? priority)
    {
        var normalized = (priority ?? "").Trim().ToLowerInvariant();
        return normalized is "low" or "medium" or "high" ? normalized : "medium";
    }

    private sealed record RawCommentAnalysisResponse(
        string? Summary,
        List<string>? KeyPoints,
        List<RawCommentActionItem>? ActionItems,
        List<string>? Risks,
        string? SuggestedReply,
        List<string>? Warnings
    );

    private sealed record RawCommentActionItem(
        string Title,
        string? OwnerHint,
        string? Priority,
        string? Reason
    );
}
