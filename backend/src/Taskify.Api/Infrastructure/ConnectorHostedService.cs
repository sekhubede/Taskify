using Taskify.Connectors;

namespace Taskify.Api.Infrastructure;

/// <summary>
/// Verifies the data source connector is available on startup.
/// Logs warnings if the connection fails but does not prevent the API from starting.
/// </summary>
public class ConnectorHostedService : IHostedService
{
    private readonly ITaskDataSource _dataSource;
    private readonly ILogger<ConnectorHostedService> _logger;

    public ConnectorHostedService(ITaskDataSource dataSource, ILogger<ConnectorHostedService> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Verifying data source connector availability...");

        try
        {
            var available = await _dataSource.IsAvailableAsync();
            if (available)
            {
                _logger.LogInformation("Data source connector is available and ready");
            }
            else
            {
                _logger.LogWarning("Data source connector reported as unavailable. Some operations may fail.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify data source connector. API will continue but data operations may fail.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_dataSource is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return Task.CompletedTask;
    }
}
