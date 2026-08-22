namespace TaskFlow.Api.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public User(Guid id, string name, string email, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            throw new ArgumentException(
                "User name must be non-blank and at most 200 characters.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(email) || email.Length > 200)
        {
            throw new ArgumentException(
                "User email must be non-blank and at most 200 characters.", nameof(email));
        }

        Id = id;
        Name = name;
        Email = email;
        CreatedAt = createdAt;
    }

#pragma warning disable CS8618 // required for future EF Core materialization; all properties set via reflection.
    private User()
    {
    }
#pragma warning restore CS8618
}
