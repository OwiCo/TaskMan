using TaskFlow.Api.Domain.Entities;

namespace TaskFlow.Api.Contracts.Mapping;

public static class UserMappings
{
    public static UserResponse ToDto(this User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.CreatedAt);
}
