using Taskify.Domain.Entities;

namespace Taskify.Domain.Interfaces;
public interface IVaultConnectionManager
{
    /// <summary>
    /// Connects to a vault and stores the connection details in the manager.
    /// </summary>
    void Connect(string vaultGuid);

    /// <summary>
    /// Gets the current vault connection details.
    /// </summary>
    Vault GetVaultInfo();

    /// <summary>
    /// Gets the raw M-Files vault connection for performing operations.
    /// Returns object type to avoid domain layer dependency on MFilesAPI.
    /// Cast to MFilesAPI.Vault in infrastructure layer.
    /// </summary>
    object GetVaultConnection();
}