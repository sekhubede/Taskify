using Taskify.Domain.Entities;

namespace Taskify.Domain.Interfaces;

public interface IVaultConnectionContext
{
    Vault CurrentVault { get; }
    User CurrentUser { get; }
    bool HasActiveConnection { get; }

    void SetConnection(Vault vault, User user);
    void ClearConnection();
}