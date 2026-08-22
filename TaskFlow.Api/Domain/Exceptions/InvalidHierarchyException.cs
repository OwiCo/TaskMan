using TaskFlow.Api.Domain.Entities;

namespace TaskFlow.Api.Domain.Exceptions;

public sealed class InvalidHierarchyException(string message, IssueType childType, IssueType? parentType)
    : Exception(message)
{
    public IssueType ChildType { get; } = childType;
    public IssueType? ParentType { get; } = parentType;
}
