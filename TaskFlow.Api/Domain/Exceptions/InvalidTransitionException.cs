using TaskFlow.Api.Domain.Entities;

namespace TaskFlow.Api.Domain.Exceptions;

public sealed class InvalidTransitionException(WorkItemStatus from, WorkItemStatus to)
    : Exception($"Cannot transition from {from} to {to}.")
{
    public WorkItemStatus From { get; } = from;
    public WorkItemStatus To { get; } = to;
}
