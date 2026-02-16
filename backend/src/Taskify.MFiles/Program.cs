using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Taskify.Connectors;
using Taskify.Application.Assignments.Services;
using Taskify.Application.Comments.Services;
using Taskify.Application.Subtasks.Services;
using Taskify.Domain.Interfaces;
using Taskify.Infrastructure.Storage;

namespace Taskify.MFiles;

public class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var loggerFactory = LoggerFactory.Create(b =>
        {
            b.AddConsole();
            b.AddDebug();
            b.SetMinimumLevel(LogLevel.Information);
        });

        var factory = new ConnectorFactory(configuration, loggerFactory.CreateLogger<ConnectorFactory>());
        var dataSource = factory.Create();

        var subtaskStore = new SubtaskStore();
        var subtaskNoteStore = new SubtaskNoteStore();
        var localCommentStore = new LocalCommentStore();
        var subtaskRepo = new LocalSubtaskRepository(subtaskStore, subtaskNoteStore);
        var commentRepo = new LocalCommentRepository(localCommentStore);
        var subtaskLoader = new SubtaskLoader(subtaskRepo);

        var assignmentService = new AssignmentService(dataSource, subtaskLoader);
        var commentService = new CommentService(commentRepo);
        var subtaskService = new SubtaskService(subtaskRepo);

        await RunConsoleTests(dataSource, assignmentService, commentService, subtaskService);
    }

    private static async Task RunConsoleTests(
        ITaskDataSource dataSource,
        AssignmentService assignmentService,
        CommentService commentService,
        SubtaskService subtaskService)
    {
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║   Taskify - Connector Testing Mode     ║");
        Console.WriteLine("╚════════════════════════════════════════╝\n");

        try
        {
            await TestConnectorHealth(dataSource);
            TestAssignmentRetrieval(assignmentService, commentService);
            TestSubtaskOperations(assignmentService, subtaskService);
            await TestCommentOperations(dataSource, assignmentService, commentService);

            Console.WriteLine("\n✓ All tests passed!");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n✗ Test failed: {ex.Message}");
            Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    private static async Task TestConnectorHealth(ITaskDataSource dataSource)
    {
        Console.WriteLine("[TEST 1] Connector Health Check");
        Console.WriteLine("────────────────────────────────");

        Console.Write("Checking connector availability... ");
        var available = await dataSource.IsAvailableAsync();
        Console.WriteLine(available ? "✓ Available" : "✗ Unavailable");

        Console.Write("Getting current user... ");
        var userName = await dataSource.GetCurrentUserNameAsync();
        Console.WriteLine($"✓ {userName}");
    }

    private static void TestAssignmentRetrieval(AssignmentService assignmentService, CommentService commentService)
    {
        Console.WriteLine("\n[TEST 2] Assignment Retrieval");
        Console.WriteLine("──────────────────────────────");

        Console.Write("Fetching assignments... ");
        var assignments = assignmentService.GetUserAssignments();
        Console.WriteLine($"✓ Found {assignments.Count}");

        if (assignments.Any())
        {
            Console.WriteLine("\nAssignment Summary:");
            var summary = assignmentService.GetAssignmentSummary();
            Console.WriteLine($"  Total: {summary.TotalAssignments}");
            Console.WriteLine($"  Completed: {summary.CompletedAssignments}");
            Console.WriteLine($"  Overdue: {summary.OverdueAssignments}");
            Console.WriteLine($"  Due Soon: {summary.DueSoonAssignments}");

            Console.WriteLine("\nFirst 5 assignments:");
            foreach (var assignment in assignments.Take(5))
            {
                var status = assignment.IsOverdue() ? "⚠ OVERDUE" :
                            assignment.IsDueSoon() ? "⏰ DUE SOON" :
                            assignment.Status.ToString();

                var commentCount = commentService.GetCommentCount(assignment.Id);
                var subtaskInfo = assignment.HasSubtasks()
                    ? $"{assignment.GetCompletedSubtaskCount()}/{assignment.Subtasks.Count}"
                    : "0";

                Console.WriteLine($"\n  #{assignment.Id} - {assignment.Title}");
                Console.WriteLine($"    Status: {status}");
                Console.WriteLine($"    Due: {assignment.DueDate?.ToString("yyyy-MM-dd") ?? "No due date"}");
                Console.WriteLine($"    Assigned to: {assignment.AssignedTo}");
                Console.WriteLine($"    Comments: {commentCount} | Subtasks: {subtaskInfo} | Progress: {assignment.GetCompletionPercentage()}%");

                if (!string.IsNullOrWhiteSpace(assignment.Description))
                    Console.WriteLine($"    Description: {assignment.Description.Substring(0, Math.Min(100, assignment.Description.Length))}...");
            }
        }
        else
        {
            Console.WriteLine("  No assignments found for current user.");
        }
    }

    private static void TestSubtaskOperations(AssignmentService assignmentService, SubtaskService subtaskService)
    {
        Console.WriteLine("\n[TEST 3] Subtask Operations");
        Console.WriteLine("────────────────────────────");

        var assignments = assignmentService.GetUserAssignments();
        if (!assignments.Any())
        {
            Console.WriteLine("  No assignments available to test subtasks.");
            return;
        }

        var testAssignment = assignments.First();
        Console.WriteLine($"Testing subtasks on: {testAssignment.Title} (ID: {testAssignment.Id})");

        Console.Write("\nFetching subtasks... ");
        var subtasks = subtaskService.GetSubtasksForAssignment(testAssignment.Id);
        Console.WriteLine($"✓ Found {subtasks.Count}");

        if (!subtasks.Any())
        {
            Console.Write("No subtasks found, creating a local subtask... ");
            var created = subtaskService.AddSubtask(testAssignment.Id, "First local subtask");
            Console.WriteLine($"✓ Created #{created.Id}");
            subtasks = subtaskService.GetSubtasksForAssignment(testAssignment.Id);
        }

        if (subtasks.Any())
        {
            Console.WriteLine("\nSubtasks:");
            foreach (var subtask in subtasks)
            {
                var status = subtask.IsCompleted ? "✓" : "☐";
                Console.WriteLine($"  {status} {subtask.Title}");
            }

            var firstSubtask = subtasks.First();
            Console.Write($"\nToggling completion for: {firstSubtask.Title}... ");
            var newStatus = !firstSubtask.IsCompleted;
            var success = subtaskService.ToggleSubtaskCompletion(firstSubtask.Id, newStatus);
            Console.WriteLine(success ? "✓" : "✗");
        }

        var summary = subtaskService.GetSubtaskSummary(testAssignment.Id);
        Console.WriteLine("\nSubtask Summary:");
        Console.WriteLine($"  Total: {summary.TotalSubtasks}");
        Console.WriteLine($"  Completed: {summary.CompletedSubtasks}");
        Console.WriteLine($"  Progress: {summary.CompletionPercentage}%");
    }

    private static async Task TestCommentOperations(
        ITaskDataSource dataSource,
        AssignmentService assignmentService,
        CommentService commentService)
    {
        Console.WriteLine("\n[TEST 4] Comment Operations");
        Console.WriteLine("────────────────────────────");

        var assignments = assignmentService.GetUserAssignments();
        if (!assignments.Any())
        {
            Console.WriteLine("  No assignments available to test comments.");
            return;
        }

        var testAssignment = assignments.First();
        Console.WriteLine($"Testing comments on: {testAssignment.Title} (ID: {testAssignment.Id})");

        Console.Write("\nFetching existing comments... ");
        var existingComments = commentService.GetAssignmentComments(testAssignment.Id);
        Console.WriteLine($"✓ Found {existingComments.Count}");

        if (existingComments.Any())
        {
            Console.WriteLine("\nExisting comments:");
            foreach (var comment in existingComments.Take(3))
            {
                Console.WriteLine($"  {comment.AuthorName} - {comment.CreatedDate:yyyy-MM-dd HH:mm}");
                Console.WriteLine($"    {comment.GetContentPreview(80)}");
            }
        }

        Console.Write("\nAdding test comment... ");
        var authorName = await dataSource.GetCurrentUserNameAsync();
        var newComment = commentService.AddComment(
            testAssignment.Id,
            $"Test comment from Taskify - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            authorName);
        Console.WriteLine("✓");

        var summary = commentService.GetCommentSummary(testAssignment.Id);
        Console.WriteLine("\nComment Summary:");
        Console.WriteLine($"  Total: {summary.TotalComments}");
        Console.WriteLine($"  Recent (24h): {summary.RecentComments}");
    }
}
