namespace Taskify.Application.VaultConnection.Dtos;

public record LoginRequest(string Username, string Password, string VaultGuid);

public record LoginResponse(
    string VaultId,
    string VaultName,
    string UserId,
    string UserName,
    string FullName,
    string Email
);