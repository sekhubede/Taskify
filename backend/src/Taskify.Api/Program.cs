using Taskify.Connectors;
using Taskify.Application.Assignments.Services;
using Taskify.Application.Comments.Services;
using Taskify.Application.Subtasks.Services;
using Taskify.Domain.Interfaces;
using Taskify.Infrastructure.Storage;
using Taskify.Api.Infrastructure;
using Taskify.Api.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;


var builder = WebApplication.CreateBuilder(args);
var localStorageDirectory = ResolveLocalStorageDirectory(builder.Configuration, builder.Environment.ContentRootPath);

// Logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();

    if (OperatingSystem.IsWindows())
    {
        logging.AddEventLog(settings =>
        {
            settings.SourceName = "Taskify";
        });
    }
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// ── Data Source Connector (pluggable via config) ──
builder.Services.AddSingleton<ConnectorFactory>();
builder.Services.AddSingleton<ITaskDataSource>(sp =>
{
    var factory = sp.GetRequiredService<ConnectorFactory>();
    return factory.Create();
});

// ── Local Storage (Taskify-owned data: subtasks, notes, flags, board) ──
builder.Services.AddSingleton(new SubtaskStore(localStorageDirectory));
builder.Services.AddSingleton(new SubtaskNoteStore(localStorageDirectory));
builder.Services.AddSingleton(new CommentNoteStore(localStorageDirectory));
builder.Services.AddSingleton(new CommentFlagStore(localStorageDirectory));
builder.Services.AddSingleton(new CommentSubtaskStore(localStorageDirectory));
builder.Services.AddSingleton(new QuickTaskStore(localStorageDirectory));
builder.Services.AddSingleton(new AssignmentBoardStore(localStorageDirectory));
builder.Services.AddSingleton(new WorkingOnStore(localStorageDirectory));

// ── Repositories ──
builder.Services.AddScoped<ISubtaskRepository, LocalSubtaskRepository>();
builder.Services.AddScoped<ISubtaskLoader, SubtaskLoader>();

// ── Application Services ──
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<SubtaskService>();
builder.Services.AddScoped<CommentNoteService>();
builder.Services.AddScoped<CommentFlagService>();
builder.Services.AddScoped<CommentSubtaskService>();
builder.Services.AddScoped<QuickTaskService>();
builder.Services.AddScoped<AssignmentBoardService>();
builder.Services.AddScoped<WorkingOnService>();
builder.Services.Configure<TaskifyAiOptions>(builder.Configuration.GetSection("TaskifyAI"));
builder.Services.AddHttpClient<IAiProvider, OllamaAiProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<TaskifyAiOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.OllamaBaseUrl) &&
        Uri.TryCreate(options.OllamaBaseUrl, UriKind.Absolute, out var baseUri))
    {
        client.BaseAddress = baseUri;
    }

    var timeoutSeconds = options.TimeoutSeconds <= 0 ? 25 : options.TimeoutSeconds;
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});
builder.Services.AddScoped<IAiAnalysisService, AiAnalysisService>();

// Verify connector on startup
builder.Services.AddHostedService<ConnectorHostedService>();
builder.Services.AddMemoryCache();

var app = builder.Build();
const string CommentCountsMineCacheKey = "comment-counts:mine";
const string CommentCountsAllCacheKey = "comment-counts:all";
const string AttachmentCountsMineCacheKey = "attachment-counts:mine";
const string AttachmentCountsAllCacheKey = "attachment-counts:all";

app.UseCors();

// ─── Health ───
app.MapGet("api/health", async (ITaskDataSource ds) =>
{
    var available = await ds.IsAvailableAsync();
    return Results.Ok(new { status = available ? "ok" : "degraded" });
});

// ─── Current User ───
app.MapGet("api/user/current", async (ITaskDataSource ds) =>
{
    try
    {
        var userName = await ds.GetCurrentUserNameAsync();
        return Results.Ok(new { userName });
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error getting current user: {ex.Message}");
    }
});

// ─── Assignments ───
app.MapGet("api/assignments", (bool? all, AssignmentService svc) =>
    Results.Ok(all == true ? svc.GetAllAssignments() : svc.GetUserAssignments()));
app.MapGet("api/assignments/{id:int}", (int id, AssignmentService svc) =>
{
    var a = svc.GetAssignment(id);
    return a is null ? Results.NotFound() : Results.Ok(a);
});
app.MapPost("api/assignments/{id:int}/complete", (int id, AssignmentService svc) =>
{
    var ok = svc.CompleteAssignment(id);
    return ok ? Results.Ok() : Results.BadRequest();
});
app.MapGet("api/assignments/{id:int}/board-column", (int id, AssignmentBoardService svc) =>
{
    var column = svc.GetAssignmentColumn(id);
    return Results.Ok(new { column });
});
app.MapPut("api/assignments/{id:int}/board-column", async (int id, AssignmentBoardService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<UpdateBoardColumnRequest>();
    if (body == null)
        return Results.BadRequest("body required");
    svc.SetAssignmentColumn(id, body.Column);
    return Results.Ok();
});
app.MapGet("api/assignments/board-positions", (AssignmentBoardService svc) =>
{
    var positions = svc.GetAllPositions();
    return Results.Ok(positions);
});
app.MapGet("api/assignments/{id:int}/working-on", (int id, WorkingOnService svc) =>
{
    var isWorkingOn = svc.IsWorkingOn(id);
    return Results.Ok(new { isWorkingOn });
});
app.MapPut("api/assignments/{id:int}/working-on", async (int id, WorkingOnService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<UpdateWorkingOnRequest>();
    if (body == null)
        return Results.BadRequest("body required");
    svc.SetWorkingOn(id, body.IsWorkingOn);
    return Results.Ok();
});
app.MapGet("api/assignments/working-on", (WorkingOnService svc) =>
{
    var workingOnIds = svc.GetAllWorkingOn();
    return Results.Ok(workingOnIds.ToList());
});

// ─── Attachments ───
app.MapGet("api/assignments/{id:int}/attachments", async (int id, ITaskDataSource ds) =>
{
    try
    {
        var attachments = await ds.GetAttachmentsForTaskAsync(id.ToString());
        return Results.Ok(attachments);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error getting attachments: {ex.Message}");
    }
});
app.MapPost("api/assignments/{id:int}/attachments", async (int id, ITaskDataSource ds, IMemoryCache cache, HttpRequest req) =>
{
    if (!req.HasFormContentType)
        return Results.BadRequest("multipart/form-data required");

    var form = await req.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file == null || file.Length == 0)
        return Results.BadRequest("file is required");

    await using var stream = file.OpenReadStream();
    using var ms = new MemoryStream();
    await stream.CopyToAsync(ms);

    try
    {
        var attachment = await ds.AddAttachmentAsync(
            id.ToString(),
            file.FileName,
            file.ContentType ?? "application/octet-stream",
            ms.ToArray());
        cache.Remove(AttachmentCountsMineCacheKey);
        cache.Remove(AttachmentCountsAllCacheKey);
        return Results.Ok(attachment);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error adding attachment: {ex.Message}");
    }
});
app.MapGet("api/assignments/{id:int}/attachments/{attachmentId}/download", async (int id, string attachmentId, ITaskDataSource ds) =>
{
    try
    {
        var file = await ds.GetAttachmentFileAsync(id.ToString(), attachmentId);
        if (file == null)
            return Results.NotFound();

        return Results.File(file.Content, file.ContentType, file.FileName);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error downloading attachment: {ex.Message}");
    }
});

// ─── Comments ───
app.MapGet("api/assignments/{id:int}/comments", (int id, CommentService svc) =>
    Results.Ok(svc.GetAssignmentComments(id)));
app.MapGet("api/assignments/comments/counts", async (bool? all, AssignmentService assignmentSvc, CommentService commentSvc, IMemoryCache cache) =>
{
    var cacheKey = all == true ? CommentCountsAllCacheKey : CommentCountsMineCacheKey;
    try
    {
        var counts = await GetOrCreateCommentCountsAsync(
            cache,
            cacheKey,
            () => BuildCommentCounts(all == true, assignmentSvc, commentSvc)
        );
        return Results.Ok(counts);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error getting comment counts: {ex.Message}");
    }
});
app.MapGet("api/assignments/attachments/counts", async (bool? all, AssignmentService assignmentSvc, ITaskDataSource ds, IMemoryCache cache) =>
{
    var cacheKey = all == true ? AttachmentCountsAllCacheKey : AttachmentCountsMineCacheKey;
    try
    {
        var counts = await GetOrCreateAttachmentCountsAsync(
            cache,
            cacheKey,
            async () => await BuildAttachmentCountsAsync(all == true, assignmentSvc, ds)
        );
        return Results.Ok(counts);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error getting attachment counts: {ex.Message}");
    }
});
app.MapPost("api/assignments/{id:int}/comments", async (int id, CommentService svc, IMemoryCache cache, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<AddCommentRequest>();
    if (body == null || string.IsNullOrWhiteSpace(body.Content))
        return Results.BadRequest("content is required");

    var c = svc.AddComment(id, body.Content.Trim());
    cache.Remove(CommentCountsMineCacheKey);
    cache.Remove(CommentCountsAllCacheKey);
    return Results.Ok(c);
});
app.MapPost("api/ai/comment-analysis", async (
    CommentAnalysisRequest body,
    IAiAnalysisService svc,
    CancellationToken cancellationToken) =>
{
    try
    {
        var analysis = await svc.AnalyzeCommentsAsync(body, cancellationToken);
        return Results.Ok(analysis);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (TaskCanceledException ex)
    {
        return Results.Problem(
            title: "AI request timed out",
            detail: ex.Message,
            statusCode: StatusCodes.Status504GatewayTimeout);
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem(
            title: "AI provider unavailable",
            detail: ex.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Problem(
            title: "Invalid AI response",
            detail: ex.Message,
            statusCode: StatusCodes.Status502BadGateway);
    }
});
app.MapGet("api/comments/{id:int}/note", (int id, CommentNoteService svc) =>
{
    var note = svc.GetCommentNoteItem(id);
    return Results.Ok(new
    {
        note = note?.Note,
        createdDate = note?.CreatedDate,
        updatedDate = note?.UpdatedDate
    });
});
app.MapPut("api/comments/{id:int}/note", async (int id, CommentNoteService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<UpdateCommentNoteRequest>();
    if (body == null)
        return Results.BadRequest("body required");
    try
    {
        var updated = svc.UpdateCommentNote(id, body.Note);
        return Results.Ok(new
        {
            note = updated?.Note,
            createdDate = updated?.CreatedDate,
            updatedDate = updated?.UpdatedDate
        });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapGet("api/subtasks/{id:int}/note", (int id, SubtaskNoteStore store) =>
{
    var note = store.GetNoteItem(id);
    return Results.Ok(new
    {
        note = note?.Note,
        createdDate = note?.CreatedDate,
        updatedDate = note?.UpdatedDate
    });
});
app.MapGet("api/comments/{id:int}/flag", (int id, CommentFlagService svc) =>
{
    var isFlagged = svc.IsCommentFlagged(id);
    return Results.Ok(new { isFlagged });
});
app.MapPut("api/comments/{id:int}/flag", async (int id, CommentFlagService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<UpdateCommentFlagRequest>();
    if (body == null)
        return Results.BadRequest("body required");
    svc.SetCommentFlag(id, body.IsFlagged);
    return Results.Ok();
});
app.MapGet("api/assignments/{assignmentId:int}/comments/{id:int}/subtasks", (int assignmentId, int id, CommentSubtaskService svc) =>
{
    try
    {
        var subtasks = svc.GetCommentSubtasks(assignmentId, id);
        return Results.Ok(subtasks);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapPost("api/assignments/{assignmentId:int}/comments/{id:int}/subtasks", async (int assignmentId, int id, CommentSubtaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<AddCommentSubtaskRequest>();
    if (body == null || string.IsNullOrWhiteSpace(body.Title))
        return Results.BadRequest("title is required");

    try
    {
        var subtask = svc.AddCommentSubtask(assignmentId, id, body.Title, body.Order);
        return Results.Ok(subtask);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapPost("api/comments/subtasks/{id:int}/toggle", async (int id, CommentSubtaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<ToggleCommentSubtaskRequest>();
    if (body == null)
        return Results.BadRequest("body required");

    try
    {
        var ok = svc.ToggleCommentSubtaskCompletion(id, body.IsCompleted);
        return ok ? Results.Ok() : Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapPut("api/comments/subtasks/{id:int}/title", async (int id, CommentSubtaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<UpdateCommentSubtaskTitleRequest>();
    if (body == null)
        return Results.BadRequest("body required");

    try
    {
        var ok = svc.UpdateCommentSubtaskTitle(id, body.Title);
        return ok ? Results.Ok() : Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapPut("api/assignments/{assignmentId:int}/comments/{id:int}/subtasks/reorder", async (int assignmentId, int id, CommentSubtaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<ReorderCommentSubtasksRequest>();
    if (body == null || body.SubtaskOrders == null || body.SubtaskOrders.Count == 0)
        return Results.BadRequest("subtaskOrders required");

    try
    {
        svc.ReorderCommentSubtasks(assignmentId, id, body.SubtaskOrders);
        return Results.Ok();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapDelete("api/comments/subtasks/{id:int}", (int id, CommentSubtaskService svc) =>
{
    try
    {
        var ok = svc.DeleteCommentSubtask(id);
        return ok ? Results.Ok() : Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

// ─── Subtasks ───
app.MapGet("api/assignments/{id:int}/subtasks", (int id, SubtaskService svc) =>
    Results.Ok(svc.GetSubtasksForAssignment(id)));
app.MapPost("api/assignments/{id:int}/subtasks", async (int id, SubtaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<AddSubtaskRequest>();
    if (body == null || string.IsNullOrWhiteSpace(body.Title))
        return Results.BadRequest("title is required");
    var s = svc.AddSubtask(id, body.Title.Trim(), body.Order);
    return Results.Ok(s);
});
app.MapPost("api/subtasks/{id:int}/toggle", async (int id, SubtaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<ToggleSubtaskRequest>();
    if (body == null)
        return Results.BadRequest("body required");
    var ok = svc.ToggleSubtaskCompletion(id, body.IsCompleted);
    return ok ? Results.Ok() : Results.BadRequest();
});
app.MapPut("api/subtasks/{id:int}/title", async (int id, SubtaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<UpdateSubtaskTitleRequest>();
    if (body == null)
        return Results.BadRequest("body required");

    try
    {
        var ok = svc.UpdateSubtaskTitle(id, body.Title);
        return ok ? Results.Ok() : Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapPut("api/subtasks/{id:int}/note", async (int id, SubtaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<UpdateNoteRequest>();
    if (body == null)
        return Results.BadRequest("body required");
    if (string.IsNullOrWhiteSpace(body.Note))
        svc.RemovePersonalNote(id);
    else
        svc.AddPersonalNote(id, body.Note);
    return Results.Ok();
});
app.MapPut("api/assignments/{id:int}/subtasks/reorder", async (int id, SubtaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<ReorderSubtasksRequest>();
    if (body == null || body.SubtaskOrders == null || body.SubtaskOrders.Count == 0)
        return Results.BadRequest("subtaskOrders required");
    try
    {
        svc.ReorderSubtasks(id, body.SubtaskOrders);
        return Results.Ok();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapDelete("api/subtasks/{id:int}", (int id, SubtaskService svc) =>
{
    try
    {
        var ok = svc.DeleteSubtask(id);
        return ok ? Results.Ok() : Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

// ─── Quick Tasks (local-only) ───
app.MapGet("api/quick-tasks", (QuickTaskService svc) =>
    Results.Ok(svc.GetTasks()));
app.MapPost("api/quick-tasks", async (QuickTaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<AddQuickTaskRequest>();
    if (body == null || string.IsNullOrWhiteSpace(body.Title))
        return Results.BadRequest("title is required");

    try
    {
        var task = svc.AddTask(body.Title);
        return Results.Ok(task);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapPut("api/quick-tasks/{id:int}/title", async (int id, QuickTaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<UpdateQuickTaskTitleRequest>();
    if (body == null)
        return Results.BadRequest("body required");

    try
    {
        var ok = svc.UpdateTaskTitle(id, body.Title);
        return ok ? Results.Ok() : Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapPost("api/quick-tasks/{id:int}/toggle", async (int id, QuickTaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<ToggleQuickTaskRequest>();
    if (body == null)
        return Results.BadRequest("body required");

    try
    {
        var ok = svc.ToggleTaskCompletion(id, body.IsCompleted);
        return ok ? Results.Ok() : Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapDelete("api/quick-tasks/{id:int}", (int id, QuickTaskService svc) =>
{
    try
    {
        var ok = svc.DeleteTask(id);
        return ok ? Results.Ok() : Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapGet("api/quick-tasks/{id:int}/comments", (int id, QuickTaskService svc) =>
{
    try
    {
        return Results.Ok(svc.GetTaskComments(id));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapPost("api/quick-tasks/{id:int}/comments", async (int id, QuickTaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<AddQuickTaskCommentRequest>();
    if (body == null || string.IsNullOrWhiteSpace(body.Content))
        return Results.BadRequest("content is required");

    try
    {
        var comment = svc.AddTaskComment(id, body.Content);
        return Results.Ok(comment);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapPut("api/quick-task-comments/{id:int}/content", async (int id, QuickTaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<UpdateQuickTaskCommentContentRequest>();
    if (body == null)
        return Results.BadRequest("body required");

    try
    {
        var ok = svc.UpdateTaskComment(id, body.Content);
        return ok ? Results.Ok() : Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapDelete("api/quick-task-comments/{id:int}", (int id, QuickTaskService svc) =>
{
    try
    {
        var ok = svc.DeleteTaskComment(id);
        return ok ? Results.Ok() : Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapGet("api/quick-tasks/{id:int}/checklist", (int id, QuickTaskService svc) =>
{
    try
    {
        return Results.Ok(svc.GetTaskChecklist(id));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapPost("api/quick-tasks/{id:int}/checklist", async (int id, QuickTaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<AddQuickTaskChecklistItemRequest>();
    if (body == null || string.IsNullOrWhiteSpace(body.Title))
        return Results.BadRequest("title is required");

    try
    {
        var item = svc.AddChecklistItem(id, body.Title, body.Order);
        return Results.Ok(item);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapPost("api/quick-task-checklist/{id:int}/toggle", async (int id, QuickTaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<ToggleQuickTaskChecklistItemRequest>();
    if (body == null)
        return Results.BadRequest("body required");

    try
    {
        var ok = svc.ToggleChecklistItem(id, body.IsCompleted);
        return ok ? Results.Ok() : Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapPut("api/quick-task-checklist/{id:int}/title", async (int id, QuickTaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<UpdateQuickTaskChecklistItemTitleRequest>();
    if (body == null)
        return Results.BadRequest("body required");

    try
    {
        var ok = svc.UpdateChecklistItemTitle(id, body.Title);
        return ok ? Results.Ok() : Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapPut("api/quick-tasks/{id:int}/checklist/reorder", async (int id, QuickTaskService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<ReorderQuickTaskChecklistRequest>();
    if (body == null || body.ChecklistOrders == null || body.ChecklistOrders.Count == 0)
        return Results.BadRequest("checklistOrders required");

    try
    {
        svc.ReorderChecklist(id, body.ChecklistOrders);
        return Results.Ok();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapDelete("api/quick-task-checklist/{id:int}", (int id, QuickTaskService svc) =>
{
    try
    {
        var ok = svc.DeleteChecklistItem(id);
        return ok ? Results.Ok() : Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

// Warm slow comment counts in the background so initial UI badges appear faster
// after startup and auto-refresh has warm cache to read from.
_ = Task.Run(async () =>
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    using var scope = app.Services.CreateScope();
    var assignmentSvc = scope.ServiceProvider.GetRequiredService<AssignmentService>();
    var commentSvc = scope.ServiceProvider.GetRequiredService<CommentService>();
    var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CommentCountsWarmup");

    try
    {
        await GetOrCreateCommentCountsAsync(
            cache,
            CommentCountsMineCacheKey,
            () => BuildCommentCounts(includeAll: false, assignmentSvc, commentSvc)
        );
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to warm mine comment counts cache");
    }

    try
    {
        await GetOrCreateCommentCountsAsync(
            cache,
            CommentCountsAllCacheKey,
            () => BuildCommentCounts(includeAll: true, assignmentSvc, commentSvc)
        );
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to warm all comment counts cache");
    }
});

app.Run();

static Dictionary<int, int> BuildCommentCounts(bool includeAll, AssignmentService assignmentSvc, CommentService commentSvc)
{
    var assignments = includeAll
        ? assignmentSvc.GetAllAssignments()
        : assignmentSvc.GetUserAssignments();
    return assignments.ToDictionary(
        a => a.Id,
        a => commentSvc.GetCommentCount(a.Id)
    );
}

static async Task<Dictionary<int, int>> GetOrCreateCommentCountsAsync(
    IMemoryCache cache,
    string cacheKey,
    Func<Dictionary<int, int>> factory)
{
    var counts = await cache.GetOrCreateAsync(cacheKey, entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
        return Task.FromResult(factory());
    });
    return counts ?? new Dictionary<int, int>();
}

static async Task<Dictionary<int, int>> BuildAttachmentCountsAsync(
    bool includeAll,
    AssignmentService assignmentSvc,
    ITaskDataSource ds)
{
    var assignments = includeAll
        ? assignmentSvc.GetAllAssignments()
        : assignmentSvc.GetUserAssignments();

    var counts = new Dictionary<int, int>();
    foreach (var assignment in assignments)
    {
        var attachments = await ds.GetAttachmentsForTaskAsync(assignment.Id.ToString());
        counts[assignment.Id] = attachments.Count;
    }

    return counts;
}

static async Task<Dictionary<int, int>> GetOrCreateAttachmentCountsAsync(
    IMemoryCache cache,
    string cacheKey,
    Func<Task<Dictionary<int, int>>> factory)
{
    var counts = await cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
        return await factory();
    });

    return counts ?? new Dictionary<int, int>();
}

static string ResolveLocalStorageDirectory(IConfiguration configuration, string contentRootPath)
{
    // Optional override in appsettings: "Taskify:LocalStorageDirectory"
    var configuredPath = configuration["Taskify:LocalStorageDirectory"];
    if (!string.IsNullOrWhiteSpace(configuredPath))
    {
        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
    }

    // Try known historical locations first to avoid switching datasets
    // between dotnet run and published dll executions.
    var candidateDirectories = new[]
    {
        Path.Combine(contentRootPath, "publish", "api", "storage"),
        Path.GetFullPath(Path.Combine(contentRootPath, "..", "publish", "api", "storage")),
        Path.GetFullPath(Path.Combine(contentRootPath, "..", "..", "publish", "api", "storage")),
        Path.Combine(contentRootPath, "storage"),
        Path.GetFullPath(Path.Combine(contentRootPath, "..", "storage")),
        Path.GetFullPath(Path.Combine(contentRootPath, "..", "..", "storage"))
    }
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToList();

    foreach (var candidate in candidateDirectories)
    {
        if (Directory.Exists(candidate))
        {
            return candidate;
        }
    }

    return Path.Combine(contentRootPath, "storage");
}

public record AddCommentRequest(string Content);
public record AddSubtaskRequest(string Title, int? Order);
public record AddCommentSubtaskRequest(string Title, int? Order);
public record ToggleSubtaskRequest(bool IsCompleted);
public record ToggleCommentSubtaskRequest(bool IsCompleted);
public record UpdateSubtaskTitleRequest(string Title);
public record UpdateCommentSubtaskTitleRequest(string Title);
public record UpdateNoteRequest(string? Note);
public record UpdateCommentNoteRequest(string? Note);
public record UpdateCommentFlagRequest(bool IsFlagged);
public record ReorderSubtasksRequest(Dictionary<int, int> SubtaskOrders);
public record ReorderCommentSubtasksRequest(Dictionary<int, int> SubtaskOrders);
public record UpdateBoardColumnRequest(string? Column);
public record UpdateWorkingOnRequest(bool IsWorkingOn);
public record AddQuickTaskRequest(string Title);
public record UpdateQuickTaskTitleRequest(string Title);
public record ToggleQuickTaskRequest(bool IsCompleted);
public record AddQuickTaskCommentRequest(string Content);
public record UpdateQuickTaskCommentContentRequest(string Content);
public record AddQuickTaskChecklistItemRequest(string Title, int? Order);
public record ToggleQuickTaskChecklistItemRequest(bool IsCompleted);
public record UpdateQuickTaskChecklistItemTitleRequest(string Title);
public record ReorderQuickTaskChecklistRequest(Dictionary<int, int> ChecklistOrders);
