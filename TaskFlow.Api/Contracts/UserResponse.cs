namespace TaskFlow.Api.Contracts;

public sealed record UserResponse(Guid Id, string Name, string Email, DateTimeOffset CreatedAt);
