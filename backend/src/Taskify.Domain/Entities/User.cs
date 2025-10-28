namespace Taskify.Domain.Entities;

public class User
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }

    public User(string id, string userName, string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException("User ID cannot be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentNullException("User name cannot be empty", nameof(userName));

        Id = id;
        UserName = userName;
        FullName = fullName;
        Email = email;
    }
}