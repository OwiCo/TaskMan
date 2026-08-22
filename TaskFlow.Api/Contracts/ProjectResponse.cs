namespace TaskFlow.Api.Contracts;

public sealed record ProjectResponse(
    Guid Id,
    string Key,
    string Name,
    int NextItemNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
