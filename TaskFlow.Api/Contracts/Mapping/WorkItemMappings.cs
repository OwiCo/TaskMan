using TaskFlow.Api.Domain.Entities;

namespace TaskFlow.Api.Contracts.Mapping;

public static class WorkItemMappings
{
    public static WorkItemResponse ToDto(this WorkItem workItem) => new(
        workItem.Id,
        workItem.ProjectId,
        workItem.IssueType,
        workItem.Title,
        workItem.Status,
        workItem.Number,
        workItem.ParentId,
        workItem.ReporterId,
        workItem.AssigneeId,
        workItem.CreatedAt,
        workItem.UpdatedAt);
}
