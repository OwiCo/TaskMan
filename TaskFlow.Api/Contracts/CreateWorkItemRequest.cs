using TaskFlow.Api.Domain.Entities;

namespace TaskFlow.Api.Contracts;

public sealed record CreateWorkItemRequest(
    Guid ProjectId,
    IssueType IssueType,
    string Title,
    Guid ReporterId,
    Guid? ParentId);
