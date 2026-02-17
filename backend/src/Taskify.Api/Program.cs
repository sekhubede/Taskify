using Taskify.Connectors;
using Taskify.Application.Assignments.Services;
using Taskify.Application.Comments.Services;
using Taskify.Application.Subtasks.Services;
using Taskify.Domain.Interfaces;
using Taskify.Infrastructure.Storage;
using Taskify.Api.Infrastructure;


var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSingleton<SubtaskStore>();
builder.Services.AddSingleton<SubtaskNoteStore>();
builder.Services.AddSingleton<CommentNoteStore>();
builder.Services.AddSingleton<CommentFlagStore>();
builder.Services.AddSingleton<AssignmentBoardStore>();
builder.Services.AddSingleton<WorkingOnStore>();

// ── Repositories ──
builder.Services.AddScoped<ISubtaskRepository, LocalSubtaskRepository>();
builder.Services.AddScoped<ISubtaskLoader, SubtaskLoader>();

// ── Application Services ──
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<SubtaskService>();
builder.Services.AddScoped<CommentNoteService>();
builder.Services.AddScoped<CommentFlagService>();
builder.Services.AddScoped<AssignmentBoardService>();
builder.Services.AddScoped<WorkingOnService>();

// Verify connector on startup
builder.Services.AddHostedService<ConnectorHostedService>();

var app = builder.Build();

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
app.MapGet("api/assignments", (AssignmentService svc) => Results.Ok(svc.GetUserAssignments()));
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

// ─── Comments ───
app.MapGet("api/assignments/{id:int}/comments", (int id, CommentService svc) =>
    Results.Ok(svc.GetAssignmentComments(id)));
app.MapGet("api/assignments/comments/counts", (AssignmentService assignmentSvc, CommentService commentSvc) =>
{
    try
    {
        var assignments = assignmentSvc.GetUserAssignments();
        var counts = assignments.ToDictionary(
            a => a.Id,
            a => commentSvc.GetCommentCount(a.Id)
        );
        return Results.Ok(counts);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error getting comment counts: {ex.Message}");
    }
});
app.MapPost("api/assignments/{id:int}/comments", async (int id, CommentService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<AddCommentRequest>();
    if (body == null || string.IsNullOrWhiteSpace(body.Content))
        return Results.BadRequest("content is required");

    var c = svc.AddComment(id, body.Content.Trim());
    return Results.Ok(c);
});
app.MapGet("api/comments/{id:int}/note", (int id, CommentNoteService svc) =>
{
    var note = svc.GetCommentNote(id);
    return Results.Ok(new { note });
});
app.MapPut("api/comments/{id:int}/note", async (int id, CommentNoteService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<UpdateCommentNoteRequest>();
    if (body == null)
        return Results.BadRequest("body required");
    try
    {
        svc.UpdateCommentNote(id, body.Note);
        return Results.Ok();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
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

app.Run();

public record AddCommentRequest(string Content);
public record AddSubtaskRequest(string Title, int? Order);
public record ToggleSubtaskRequest(bool IsCompleted);
public record UpdateNoteRequest(string? Note);
public record UpdateCommentNoteRequest(string? Note);
public record UpdateCommentFlagRequest(bool IsFlagged);
public record ReorderSubtasksRequest(Dictionary<int, int> SubtaskOrders);
public record UpdateBoardColumnRequest(string? Column);
public record UpdateWorkingOnRequest(bool IsWorkingOn);
