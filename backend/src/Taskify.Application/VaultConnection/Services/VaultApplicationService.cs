using Taskify.Application.VaultConnection.Dtos;
using Taskify.Application.VaultConnection.Interfaces;
using Taskify.Domain.Exceptions;
using Taskify.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Taskify.Application.VaultConnection.Services;

public class VaultApplicationService : IVaultApplicationService
{
    private readonly IVaultConnectionService _vaultConnectionService;
    private readonly IVaultConnectionContext _connectionContext;
    private readonly ILogger<VaultApplicationService> _logger;

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Login attempts for user: {Username}", request.Username);

        try
        {
            var vault = await _vaultConnectionService.ConnectAsync(request.Username, request.Password, request.VaultGuid);

            var user = await _vaultConnectionService.GetCurrentUserAsync(vault.Id);

            _connectionContext.SetConnection(vault, user);

            _logger.LogInformation("User {Username} logged in successfully", request.Username);

            return new LoginResponse(
                VaultId: vault.Id,
                VaultName: vault.Name,
                UserId: user.Id,
                UserName: user.UserName,
                FullName: user.FullName,
                Email: user.Email
            );
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning("Authentication failed for user {Username}: {message}", request.Username);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for user {Username}", request.Username);
            throw new VaultConnectionException($"Failed to authenticate with vault: {ex.Message}", ex);
        }
    }

    public async Task LogoutAsync()
    {
        if (!_connectionContext.HasActiveConnection)
        {
            _logger.LogWarning("Logout attempted but no active connection found");
            return;
        }
        try
        {
            var vaultId = _connectionContext.CurrentVault.Id;
            await _vaultConnectionService.DisconnectAsync(vaultId);
            _connectionContext.ClearConnection();

            _logger.LogInformation("User logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            throw new VaultConnectionException($"Failed to disconnect from vault: {ex.Message}", ex);
        }
    }

    public async Task<CurrentVaultResponse> GetCurrentVaultAsync()
    {
        if (!_connectionContext.HasActiveConnection)
        {
            _logger.LogWarning("GetCurrentVault called wihtout active connection");
            throw new VaultConnectionException("No active vault connection. Please login first.");
        }

        try
        {
            var isStillConnected = await _vaultConnectionService.IsConnectedAsync(_connectionContext.CurrentVault.Id);

            if (!isStillConnected)
            {
                _logger.LogWarning("Vault connection expired");
                _connectionContext.ClearConnection();
                throw new VaultConnectionException("Vault connection has expired. Please login again.");
            }

            return new CurrentVaultResponse(
                VaultId: _connectionContext.CurrentVault.Id,
                VaultName: _connectionContext.CurrentVault.Name,
                UserId: _connectionContext.CurrentUser.Id,
                UserName: _connectionContext.CurrentUser.UserName,
                IsConnected: true
            );
        }
        catch (VaultConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current vault");
            throw new VaultConnectionException("Failed to retrieve vault information", ex);
        }
    }
}