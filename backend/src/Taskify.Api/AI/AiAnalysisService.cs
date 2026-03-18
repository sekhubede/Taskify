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
        var generation = GetGenerationOptions(normalizedMode);

        var timer = Stopwatch.StartNew();
        var raw = await _provider.GenerateJsonAsync(systemPrompt, userPrompt, generation, cancellationToken);

        if (!TryParseResponse(raw, out var parsed))
        {
            var retryPrompt = $"{userPrompt}\n\nYour previous response was not valid JSON. Return only valid JSON for the schema.";
            var retryGeneration = generation with
            {
                // Increase budget on retry to avoid truncated JSON responses.
                NumPredict = Math.Min(generation.NumPredict + 220, 700),
                Temperature = 0.05,
                TopP = 0.85
            };
            raw = await _provider.GenerateJsonAsync(systemPrompt, retryPrompt, retryGeneration, cancellationToken);

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
You are an assistant for analyzing assignment work context.
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
- Do not invent facts not in the provided assignment details/comments/subtasks.
- If context is missing, include a warning.
- For action items, prioritize concrete next steps that move the assignment forward.
""";
    }

    private static string BuildUserPrompt(CommentAnalysisRequest request, string mode)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Mode: {mode}");
        sb.AppendLine($"Assignment Id: {request.AssignmentId}");
        sb.AppendLine($"Assignment Title: {request.AssignmentTitle}");
        sb.AppendLine($"Assignment Description: {Truncate(request.AssignmentDescription, 2400)}");
        sb.AppendLine($"Include Personal Notes: {request.IncludePersonalNotes}");
        sb.AppendLine("Existing assignment subtasks:");

        var existingSubtasks = request.ExistingSubtasks ?? [];
        var maxExistingSubtasks = mode switch
        {
            "summary" => 12,
            "actions" => 20,
            _ => 30
        };
        var maxSubtaskTitleLength = mode switch
        {
            "summary" => 150,
            "actions" => 190,
            _ => 240
        };
        if (existingSubtasks.Count == 0)
        {
            sb.AppendLine("- None");
        }
        else
        {
            foreach (var subtask in existingSubtasks.Take(maxExistingSubtasks))
            {
                var status = subtask.IsCompleted ? "Completed" : "Open";
                sb.AppendLine($"- [{status}] {Truncate(subtask.Title, maxSubtaskTitleLength)}");
            }
        }

        sb.AppendLine("Comments (oldest to newest):");

        var maxComments = mode switch
        {
            "summary" => 14,
            "actions" => 30,
            _ => 45
        };
        var maxCommentLength = mode switch
        {
            "summary" => 450,
            "actions" => 900,
            _ => 1100
        };
        var maxPersonalNoteLength = mode switch
        {
            "summary" => 180,
            "actions" => 260,
            _ => 320
        };

        foreach (var comment in request.Comments.OrderBy(c => c.CreatedDate).TakeLast(maxComments))
        {
            sb.AppendLine($"- CommentId: {comment.CommentId}");
            sb.AppendLine($"  Author: {comment.Author}");
            sb.AppendLine($"  Created: {comment.CreatedDate:O}");
            sb.AppendLine($"  Content: {Truncate(comment.Content, maxCommentLength)}");
            if (request.IncludePersonalNotes && !string.IsNullOrWhiteSpace(comment.PersonalNote))
            {
                sb.AppendLine($"  PersonalNote: {Truncate(comment.PersonalNote, maxPersonalNoteLength)}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Focus by mode:");
        sb.AppendLine("- summary: emphasize what is being asked, progress so far, blockers, and next immediate steps. Keep under 80 words.");
        sb.AppendLine("- actions: prioritize concrete action items from assignment description + current subtasks + comments. Keep actionItems to max 6 and set suggestedReply to null.");
        sb.AppendLine("- full: provide all sections, but keep each list concise.");
        return sb.ToString();
    }

    private static bool TryParseResponse(string raw, out RawCommentAnalysisResponse? parsed)
    {
        parsed = null;
        if (TryDeserialize(raw, out parsed))
        {
            return true;
        }

        foreach (var candidate in ExtractJsonObjects(raw))
        {
            if (TryDeserialize(candidate, out parsed))
            {
                return true;
            }
        }

        return false;
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

    private static AiGenerationOptions GetGenerationOptions(string mode)
    {
        return mode switch
        {
            "summary" => new AiGenerationOptions(NumPredict: 220, Temperature: 0.1, TopP: 0.9),
            "actions" => new AiGenerationOptions(NumPredict: 360, Temperature: 0.1, TopP: 0.9),
            _ => new AiGenerationOptions(NumPredict: 480, Temperature: 0.15, TopP: 0.9)
        };
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return $"{trimmed[..maxLength]}...(truncated)";
    }

    private static bool TryDeserialize(string raw, out RawCommentAnalysisResponse? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

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

    private static IEnumerable<string> ExtractJsonObjects(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            yield break;
        }

        var trimmed = raw.Trim();

        // Try fenced JSON block first (```json ... ```).
        var fenceStart = trimmed.IndexOf("```json", StringComparison.OrdinalIgnoreCase);
        if (fenceStart >= 0)
        {
            var contentStart = fenceStart + "```json".Length;
            var fenceEnd = trimmed.IndexOf("```", contentStart, StringComparison.Ordinal);
            if (fenceEnd > contentStart)
            {
                var fenced = trimmed[contentStart..fenceEnd].Trim();
                if (!string.IsNullOrWhiteSpace(fenced))
                {
                    yield return fenced;
                }
            }
        }

        // Try balanced object extraction across the raw content.
        var inString = false;
        var escaped = false;
        var depth = 0;
        var start = -1;

        for (var i = 0; i < trimmed.Length; i++)
        {
            var ch = trimmed[i];

            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (ch == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (ch == '"')
            {
                inString = true;
                continue;
            }

            if (ch == '{')
            {
                if (depth == 0)
                {
                    start = i;
                }
                depth++;
                continue;
            }

            if (ch == '}' && depth > 0)
            {
                depth--;
                if (depth == 0 && start >= 0)
                {
                    var candidate = trimmed[start..(i + 1)];
                    if (!string.IsNullOrWhiteSpace(candidate))
                    {
                        yield return candidate;
                    }
                    start = -1;
                }
            }
        }
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
