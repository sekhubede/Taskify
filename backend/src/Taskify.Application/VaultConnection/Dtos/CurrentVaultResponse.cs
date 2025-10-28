namespace Taskify.Application.VaultConnection.Dtos;

public record CurrentVaultResponse(
    string VaultId,
    string VaultName,
    string UserId,
    string UserName,
    bool IsConnected
);