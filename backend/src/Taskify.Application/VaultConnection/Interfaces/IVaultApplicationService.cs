using Taskify.Application.VaultConnection.Dtos;

namespace Taskify.Application.VaultConnection.Interfaces;

public interface IVaultApplicationService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<CurrentVaultResponse> GetCurrentVaultAsync();
    Task LogoutAsync();
}