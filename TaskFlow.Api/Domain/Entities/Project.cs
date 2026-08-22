using System.Text.RegularExpressions;

namespace TaskFlow.Api.Domain.Entities;

public sealed partial class Project
{
    public Guid Id { get; private set; }
    public string Key { get; private set; }
    public string Name { get; private set; }
    public int NextItemNumber { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Project(Guid id, string key, string name, DateTimeOffset createdAt)
    {
        if (key is null || !KeyPattern().IsMatch(key))
        {
            throw new ArgumentException(
                "Project key must be 2-10 uppercase letters.", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            throw new ArgumentException(
                "Project name must be non-blank and at most 200 characters.", nameof(name));
        }

        Id = id;
        Key = key;
        Name = name;
        NextItemNumber = 1;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    [GeneratedRegex("^[A-Z]{2,10}$")]
    private static partial Regex KeyPattern();

#pragma warning disable CS8618 // required for future EF Core materialization; all properties set via reflection.
    private Project()
    {
    }
#pragma warning restore CS8618
}
