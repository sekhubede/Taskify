using Taskify.Domain.Interfaces;

namespace Taskify.Infrastructure.MFilesInterop;

using System.Runtime.InteropServices;
using MFilesAPI;
using Taskify.Infrastructure.Mappers;

public class MFilesVaultConnectionManager : IVaultConnectionManager, IDisposable
{
    private MFilesClientApplication? _mfilesClientApp;
    private Vault? _liveVault;
    private bool _isConnected;
    private readonly object _connectionLock = new object();
    private bool _disposed;

    public MFilesVaultConnectionManager()
    {
        _mfilesClientApp = new MFilesClientApplication();
    }

    public void Connect(string vaultGuid)
    {
        lock (_connectionLock)
        {
            if (_isConnected) return;

            ObjectDisposedException.ThrowIf(_disposed, this);

            try
            {
                _liveVault = _mfilesClientApp?.GetVaultConnectionsWithGUID(vaultGuid)
                    .Cast<VaultConnection>()
                    .FirstOrDefault()?
                    .BindToVault(IntPtr.Zero, CanLogIn: true, ReturnNULLIfCancelledByUser: true);

                if (_liveVault == null)
                    throw new InvalidOperationException($"Failed to bind to vault {vaultGuid}. Vault may not exist or login was cancelled by user.");

                _isConnected = true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _liveVault = null;

                throw new ApplicationException($"Failed to connect to M-Files vault {vaultGuid}", ex);
            }
        }
    }

    public Domain.Entities.Vault GetVaultInfo()
    {
        lock (_connectionLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_isConnected || _liveVault == null)
                throw new InvalidOperationException("M-Files vault is not connected. Vall Connect() first.");

            var vault = MFilesDataMapper.MapToDomainVault(_liveVault);
            vault.MarkAsAuthenticated();
            return vault;
        }
    }

    public object GetVaultConnection()
    {
        lock (_connectionLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_isConnected || _liveVault == null)
                throw new InvalidOperationException("M-Files vault is not connected. Connect() first.");

            return _liveVault;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_connectionLock)
        {

            if (_liveVault != null)
            {
                Marshal.ReleaseComObject(_liveVault);
                _liveVault = null;
            }

            if (_mfilesClientApp != null)
            {
                Marshal.ReleaseComObject(_mfilesClientApp);
                _mfilesClientApp = null;
            }

            _isConnected = false;
            _disposed = true;
        }

    }
}