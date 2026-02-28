namespace EventFlow.AuthService.Domain.Entities;

public class Role
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ICollection<User> Users { get; private set; } = [];

    private Role() { }

    public static Role Create(string name) => new() { Id = Guid.NewGuid(), Name = name };

    public static class Names
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }
}
