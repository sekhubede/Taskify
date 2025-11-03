using Autofac;
using Taskify.Application.VaultConnection.Services;
using Taskify.Domain.Interfaces;
using Taskify.Infrastructure.MFilesInterop;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Taskify.MFiles.Configuration;
using Microsoft.Extensions.Logging;
using Taskify.Application.Assignments.Services;
using Taskify.Domain.Entities;
using Taskify.Infrastructure.Mappers;
namespace Taskify.MFiles;

public class Program
{
    public static void Main(string[] args)
    {
        var container = BuildContainer();

        using var scope = container.BeginLifetimeScope();

        RunConsoleTests(scope);
    }

    private static void TestVaultConnection(ILifetimeScope scope)
    {
        Debug.WriteLine("Test 1: Vault Connection");
        Debug.WriteLine("--------------------------------");

        var connectionService = scope.Resolve<VaultConnectionService>();
        var settings = scope.Resolve<MFilesSettings>();

        Debug.WriteLine($"⏳ Connecting to vault: {settings.VaultGuid}");
        connectionService.InitializeConnection(settings.VaultGuid);

        var connectionManager = scope.Resolve<IVaultConnectionManager>();
        var vault = connectionManager.GetVaultInfo();

        Debug.WriteLine($"✔ Connected to vault: {vault.Name}");
        Debug.WriteLine($"✔ Authentcated: {vault.IsAuthenticated}");
    }

    private static void RunConsoleTests(ILifetimeScope scope)
    {
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║   Taskify MVP - Manual Testing Mode    ║");
        Console.WriteLine("╚════════════════════════════════════════╝\n");

        try
        {
            // Test 1: Vault Conneciton
            TestVaultConnection(scope);

            // Test 2: Assignment Retrieval
            TestAssignmentRetrieval(scope);

            // Test 3: Comment Operations

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

        Console.WriteLine("\nPress any key to exit...");
        // Console.ReadKey();
    }

    private static void TestAssignmentRetrieval(ILifetimeScope scope)
    {
        Console.WriteLine("\n[TEST 2] Assignment Retrieval");
        Console.WriteLine("──────────────────────────────");

        var assignmentService = scope.Resolve<AssignmentService>();

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

                Console.WriteLine($"\n  #{assignment.Id} - {assignment.Title}");
                Console.WriteLine($"    Status: {status}");
                Console.WriteLine($"    Due: {assignment.DueDate?.ToString("yyyy-MM-dd") ?? "No due date"}");
                Console.WriteLine($"    Assigned to: {assignment.AssignedTo}");

                if (assignment.Id == 4)
                    assignmentService.CompleteAssignment(assignment.Id);

                if (!string.IsNullOrWhiteSpace(assignment.Description))
                    Console.WriteLine($"    Description: {assignment.Description.Substring(0, Math.Min(100, assignment.Description.Length))}...");
            }
        }
        else
        {
            Console.WriteLine("  No assignments found for current user.");
        }
    }

    private static IContainer BuildContainer()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var builder = new ContainerBuilder();

        // Register configuration
        var mfilesSettings = configuration.GetSection("MFiles").Get<MFilesSettings>()
            ?? throw new InvalidOperationException("MFiles configuration missing");
        builder.RegisterInstance(mfilesSettings).AsSelf().SingleInstance();
        builder.RegisterInstance(configuration).As<IConfiguration>();

        // Infrastructure - Vault connection manager
        builder.RegisterType<MFilesVaultConnectionManager>()
            .As<IVaultConnectionManager>()
            .AsSelf()
            .SingleInstance()
            .OnRelease(instance => instance.Dispose());

        // Infrastructure - Repository implementations
        builder.RegisterAssemblyTypes(typeof(MFilesVaultConnectionManager).Assembly)
            .Where(t => t.Name.EndsWith("Repository"))
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        // Application services
        builder.RegisterAssemblyTypes(typeof(VaultConnectionService).Assembly)
            .Where(t => t.Name.EndsWith("Service"))
            .AsSelf()
            .InstancePerLifetimeScope();

        // Logging
        builder.Register(ctx =>
        {
            var loggerFactory = LoggerFactory.Create(b =>
            {
                b.AddConsole();
                b.AddDebug();
                b.SetMinimumLevel(LogLevel.Information);
            });

            return loggerFactory;

        }).As<ILoggerFactory>().SingleInstance();

        return builder.Build();
    }
}