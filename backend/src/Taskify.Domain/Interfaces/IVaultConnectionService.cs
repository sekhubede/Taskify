using Taskify.Domain.Entities;

namespace Taskify.Domain.Interfaces;

public interface IVaultConnectionService
{
    /// <summary>
    /// Authenticates a user with M-Files vault
    /// </summary>
    Task<Vault> ConnectAsync(string username, string password, string vaultGuid);

    /// <summary>
    /// Disconnects a user from M-Files vault
    /// </summary>
    Task DisconnectAsync(string vaultId);

    /// <summary>
    /// Validates if current connection is still valid
    /// </summary>
    Task<bool> IsConnectedAsync(string vaultId);

    /// <summary>
    /// Gets current user information from vault
    /// </summary>
    Task<User> GetCurrentUserAsync(string vaultId);
}