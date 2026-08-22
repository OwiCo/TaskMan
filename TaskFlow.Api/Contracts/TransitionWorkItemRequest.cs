using TaskFlow.Api.Domain.Entities;

namespace TaskFlow.Api.Contracts;

public sealed record TransitionWorkItemRequest(WorkItemStatus To);
