using TaskFlow.Api.Domain.Entities;

namespace TaskFlow.Api.Contracts;

public sealed record WorkItemResponse(
    Guid Id,
    Guid ProjectId,
    IssueType IssueType,
    string Title,
    WorkItemStatus Status,
    int Number,
    Guid? ParentId,
    Guid ReporterId,
    Guid? AssigneeId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
