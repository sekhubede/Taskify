namespace Taskify.Domain.Entities;

public class Vault
{
    public string Id { get; set; }
    public string Name { get; set; }

    public string Guid { get; set; }

    public string ServerName { get; set; }

    public bool IsAuthenticated { get; set; }

    public Vault(string id, string name, string guid, string serverName)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException("Vault ID cannot be empty", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException("Vault name cannot be empty", nameof(name));

        Id = id;
        Name = name;
        Guid = guid;
        ServerName = serverName;
        IsAuthenticated = false;
    }

    public void MarkAsAuthenticated() => IsAuthenticated = true;
}