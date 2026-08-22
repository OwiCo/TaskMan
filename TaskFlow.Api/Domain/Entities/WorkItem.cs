using TaskFlow.Api.Domain.Exceptions;

namespace TaskFlow.Api.Domain.Entities;

public sealed class WorkItem
{
    private static readonly Dictionary<WorkItemStatus, WorkItemStatus[]> LegalTransitions = new()
    {
        [WorkItemStatus.Todo] = [WorkItemStatus.InProgress],
        [WorkItemStatus.InProgress] = [WorkItemStatus.InReview, WorkItemStatus.Todo],
        [WorkItemStatus.InReview] = [WorkItemStatus.Done, WorkItemStatus.InProgress],
        [WorkItemStatus.Done] = [],
    };

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public IssueType IssueType { get; private set; }
    public string Title { get; private set; }
    public WorkItemStatus Status { get; private set; }
    public int Number { get; private set; }
    public Guid? ParentId { get; private set; }
    public Guid ReporterId { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public WorkItem(
        Guid id,
        Guid projectId,
        IssueType issueType,
        string title,
        int number,
        Guid reporterId,
        WorkItem? parent,
        DateTimeOffset createdAt)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Work item must belong to a project.", nameof(projectId));
        }

        if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
        {
            throw new ArgumentException(
                "Work item title must be non-blank and at most 200 characters.", nameof(title));
        }

        if (number < 1)
        {
            throw new ArgumentException("Work item number must be at least 1.", nameof(number));
        }

        if (reporterId == Guid.Empty)
        {
            throw new ArgumentException("Work item must have a reporter.", nameof(reporterId));
        }

        ValidateParent(issueType, parent);

        Id = id;
        ProjectId = projectId;
        IssueType = issueType;
        Title = title;
        Status = WorkItemStatus.Todo;
        Number = number;
        ReporterId = reporterId;
        ParentId = parent?.Id;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public void Transition(WorkItemStatus to, DateTimeOffset now)
    {
        if (!LegalTransitions[Status].Contains(to))
        {
            throw new InvalidTransitionException(Status, to);
        }

        Status = to;
        UpdatedAt = now;
    }

    private static void ValidateParent(IssueType childType, WorkItem? parent)
    {
        switch (childType)
        {
            case IssueType.Epic:
                if (parent is not null)
                {
                    throw new InvalidHierarchyException(
                        "An Epic cannot have a parent.", childType, parent.IssueType);
                }
                break;

            case IssueType.Story:
            case IssueType.Task:
            case IssueType.Bug:
                if (parent is not null && parent.IssueType != IssueType.Epic)
                {
                    throw new InvalidHierarchyException(
                        $"A {childType} may only be parented by an Epic.", childType, parent.IssueType);
                }
                break;

            case IssueType.SubTask:
                if (parent is null)
                {
                    throw new InvalidHierarchyException(
                        "A Sub-task must have a parent.", childType, null);
                }
                if (parent.IssueType is IssueType.Epic or IssueType.SubTask)
                {
                    throw new InvalidHierarchyException(
                        "A Sub-task's parent must be a Story, Task, or Bug.", childType, parent.IssueType);
                }
                break;
        }
    }

#pragma warning disable CS8618 // required for future EF Core materialization; all properties set via reflection.
    private WorkItem()
    {
    }
#pragma warning restore CS8618
}
