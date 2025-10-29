namespace Taskify.Application.VaultConnection.Services;

using System.Diagnostics;
using Taskify.Domain.Interfaces;
public class VaultConnectionService
{
    private readonly IVaultConnectionManager _connectionManager;
    public VaultConnectionService(IVaultConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public void InitializeConnection(string vaultGuid)
    {
        _connectionManager.Connect(vaultGuid);
    }
}