using Autofac;
using Taskify.Application.VaultConnection.Services;
using Taskify.Domain.Interfaces;
using Taskify.Infrastructure.MFilesInterop;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Taskify.MFiles.Configuration;
using Microsoft.Extensions.Logging;
namespace Taskify.MFiles;

public class Program
{
    public static void Main(string[] args)
    {
        var container = BuildContainer();

        using var scope = container.BeginLifetimeScope();

        try
        {
            Debug.WriteLine("=== Taskify MVP Manual Testing ===");

            // Test 1: Vault Conneciton
            Debug.WriteLine("\n=== Test 1: Vault Connection ===");
            Debug.WriteLine("Attempting to connect to M-Files vault...");

            TestVaultConnection(scope);

            // Test 2: Assignment Retrieval

            // Test 3: Comment Operations

            Debug.WriteLine("\n✔ All tests passed successfully!");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"\n✖ Test Failed: {ex.Message}");
            Debug.WriteLine($"Details: {ex.StackTrace}");
        }

        Debug.WriteLine("\nPress any key to exit...");
        // Console.ReadKey(true);
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
        var vault = connectionManager.GetVault();

        Debug.WriteLine($"✔ Connected to vault: {vault.Name}");
        Debug.WriteLine($"✔ Authentcated: {vault.IsAuthenticated}");
    }

    private static void TestAssignmentRetrieval(ILifetimeScope scope)
    {
        throw new NotImplementedException();
    }

    private static IContainer BuildContainer()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var builder = new ContainerBuilder();

        // Register configuration
        var mfilesSettings = configuration.GetSection("MFiles").Get<MFilesSettings>()
            ?? throw new InvalidOperationException("MFiles configuration missing");
        builder.RegisterInstance(mfilesSettings).AsSelf();


        // Register services
        builder.RegisterAssemblyTypes(typeof(VaultConnectionService).Assembly)
            .Where(t => t.Name.EndsWith("Service"))
            .AsSelf()
            .InstancePerLifetimeScope();

        // Register infrastructure
        builder.RegisterType<MFilesVaultConnectionManager>()
            .As<IVaultConnectionManager>()
            .SingleInstance()
            .OnRelease(instance => instance.Dispose());

        // Logging
        builder.Register(ctx =>
        {
            var loggerFactory = LoggerFactory.Create(b =>
            {
                b.AddConsole();
                b.SetMinimumLevel(LogLevel.Information);
            });

            return loggerFactory;

        }).As<ILoggerFactory>().SingleInstance();

        return builder.Build();
    }
}