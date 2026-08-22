using TaskFlow.Api.Domain.Entities;

namespace TaskFlow.Api.Contracts.Mapping;

public static class ProjectMappings
{
    public static ProjectResponse ToDto(this Project project) => new(
        project.Id,
        project.Key,
        project.Name,
        project.NextItemNumber,
        project.CreatedAt,
        project.UpdatedAt);
}
