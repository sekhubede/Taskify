using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Taskify.Application.VaultConnection.Services;
using Taskify.Api.Configuration;

namespace Taskify.Api.Infrastructure;

public class MFilesConnectionHostedService : IHostedService
{
    private readonly VaultConnectionService _vaultConnectionService;
    private readonly MFilesSettings _settings;
    private readonly ILogger<MFilesConnectionHostedService> _logger;

    public MFilesConnectionHostedService(
        VaultConnectionService vaultConnectionService,
        MFilesSettings settings,
        ILogger<MFilesConnectionHostedService> logger)
    {
        _vaultConnectionService = vaultConnectionService;
        _settings = settings;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.VaultGuid))
        {
            _logger.LogWarning("MFiles: VaultGuid is missing. API will start but M-Files operations will fail.");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Connecting to M-Files vault: {VaultGuid}", _settings.VaultGuid);

        try
        {
            _vaultConnectionService.InitializeConnection(_settings.VaultGuid);
            _logger.LogInformation("Successfully connected to M-Files vault");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to M-Files vault. API will continue but M-Files operations will fail.");
            // Don't throw - allow API to start even if M-Files connection fails
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}


