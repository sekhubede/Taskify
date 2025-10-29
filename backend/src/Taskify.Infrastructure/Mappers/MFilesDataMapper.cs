namespace Taskify.Infrastructure.Mappers;

using Taskify.Domain.Entities;

public static class MFilesDataMapper
{
    public static Vault MapToDomainVault(MFilesAPI.Vault mfilesVault)
    {
        return new Vault(
            name: mfilesVault.Name,
            guid: mfilesVault.GetGUID()
        );
    }
}