using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Taskify.Application.Assignments.Services;
using Taskify.Application.Comments.Services;
using Taskify.Application.Subtasks.Services;
using Taskify.Domain.Interfaces;
using Taskify.Infrastructure.MFilesInterop;
using Taskify.Infrastructure.Storage;
using Taskify.Api.Configuration;
using Taskify.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

// Note: To bind to internal network, set "Urls": "http://0.0.0.0:5000" in appsettings.json
// or set environment variable ASPNETCORE_URLS=http://0.0.0.0:5000

// Logging: console/debug in dev; Event Log when running as a Windows service
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

// Autofac DI
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    var configuration = builder.Configuration;
    var mfilesSettings = configuration.GetSection("MFiles").Get<MFilesSettings>()
        ?? throw new InvalidOperationException("MFiles configuration missing");
    containerBuilder.RegisterInstance(mfilesSettings).AsSelf().SingleInstance();
    containerBuilder.RegisterInstance(configuration).As<IConfiguration>();

    // Vault connection manager
    containerBuilder.RegisterType<MFilesVaultConnectionManager>()
        .As<IVaultConnectionManager>()
        .AsSelf()
        .SingleInstance()
        .OnRelease(instance => instance.Dispose());

    // Repositories
    containerBuilder.RegisterType<MFilesAssignmentRepository>()
        .As<IAssignmentRepository>()
        .InstancePerLifetimeScope();
    containerBuilder.RegisterType<MFilesCommentRepository>()
        .As<ICommentRepository>()
        .InstancePerLifetimeScope();
    containerBuilder.RegisterType<SubtaskStore>()
        .AsSelf()
        .SingleInstance();
    containerBuilder.RegisterType<SubtaskNoteStore>()
        .AsSelf()
        .SingleInstance();
    containerBuilder.RegisterType<LocalSubtaskRepository>()
        .As<ISubtaskRepository>()
        .InstancePerLifetimeScope();

    // Services
    containerBuilder.RegisterAssemblyTypes(typeof(AssignmentService).Assembly)
        .Where(t => t.Name.EndsWith("Service"))
        .AsSelf()
        .InstancePerLifetimeScope();
});

// Initialize M-Files connection at startup
builder.Services.AddHostedService<MFilesConnectionHostedService>();

var app = builder.Build();

app.UseCors();

// Simple health
app.MapGet("api/health", () => Results.Ok(new { status = "ok" }));

// Assignments
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

// Comments
app.MapGet("api/assignments/{id:int}/comments", (int id, CommentService svc) =>
    Results.Ok(svc.GetAssignmentComments(id)));
app.MapPost("api/assignments/{id:int}/comments", async (int id, CommentService svc, HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<AddCommentRequest>();
    if (body == null || string.IsNullOrWhiteSpace(body.Content))
        return Results.BadRequest("content is required");
    var c = svc.AddComment(id, body.Content.Trim());
    return Results.Ok(c);
});

// Subtasks
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

app.Run();

public record AddCommentRequest(string Content);
public record AddSubtaskRequest(string Title, int? Order);
public record ToggleSubtaskRequest(bool IsCompleted);
public record UpdateNoteRequest(string? Note);


