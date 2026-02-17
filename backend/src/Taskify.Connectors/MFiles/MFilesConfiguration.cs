namespace Taskify.Connectors.MFiles;

/// <summary>
/// Configuration specific to the M-Files connection.
/// Loaded from appsettings.json.
/// </summary>
public class MFilesConfiguration
{
    public string VaultGuid { get; set; } = string.Empty;
}
