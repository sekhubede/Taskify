using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Taskify.Connectors.MFiles;
using Taskify.Connectors.Mock;

namespace Taskify.Connectors;

/// <summary>
/// Reads the config and returns the right connector.
/// The rest of the app never knows this class exists.
///
/// To switch connectors, change this in appsettings.json:
///   "Taskify": { "DataSource": "MFiles" }   --> uses M-Files COM API
///   "Taskify": { "DataSource": "Mock" }     --> uses mock data
///   "Taskify": { "DataSource": "Odoo" }     --> future
/// </summary>
public class ConnectorFactory
{
    private readonly IConfiguration _config;
    private readonly ILogger<ConnectorFactory> _logger;

    public ConnectorFactory(IConfiguration config, ILogger<ConnectorFactory> logger)
    {
        _config = config;
        _logger = logger;
    }

    public ITaskDataSource Create()
    {
        var dataSource = _config["Taskify:DataSource"] ?? "Mock";

        _logger.LogInformation("Initializing data source connector: {DataSource}", dataSource);

        return dataSource.ToLower() switch
        {
            "mfiles" => CreateMFilesConnector(),
            "mock" => new MockConnector(),
            // "odoo" => CreateOdooConnector(),
            _ => throw new InvalidOperationException(
                $"Unknown data source: '{dataSource}'. " +
                $"Valid options: MFiles, Mock. Check appsettings.json.")
        };
    }

    private ITaskDataSource CreateMFilesConnector()
    {
        var vaultGuid = _config["MFiles:VaultGuid"];

        if (string.IsNullOrWhiteSpace(vaultGuid))
            throw new InvalidOperationException(
                "MFiles:VaultGuid is required when DataSource is 'MFiles'. Check appsettings.json.");

        var config = new MFilesConfiguration
        {
            VaultGuid = vaultGuid
        };

        return new MFilesConnector(config, _logger);
    }
}
