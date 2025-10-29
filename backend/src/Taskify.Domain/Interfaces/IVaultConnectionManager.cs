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
    Vault GetVault();
}