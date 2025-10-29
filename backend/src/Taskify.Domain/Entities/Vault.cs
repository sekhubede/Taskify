namespace Taskify.Domain.Entities;

public class Vault
{
    public string Name { get; set; }

    public string Guid { get; set; }
    public bool IsAuthenticated { get; private set; }

    public Vault(string name, string guid)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Vault name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(guid))
            throw new ArgumentNullException("Vault GUID cannot be empty", nameof(guid));

        Name = name;
        Guid = guid;
        IsAuthenticated = false;
    }

    public void MarkAsAuthenticated() => IsAuthenticated = true;
}