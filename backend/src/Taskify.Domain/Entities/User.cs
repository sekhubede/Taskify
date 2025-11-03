namespace Taskify.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }

    public string FullName { get; set; }

    public User(int id, string username, string fullName)
    {
        Id = id;
        Username = username;
        FullName = fullName;
    }
}