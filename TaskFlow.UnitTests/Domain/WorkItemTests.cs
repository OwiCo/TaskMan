using TaskFlow.Api.Domain.Entities;
using TaskFlow.Api.Domain.Exceptions;

namespace TaskFlow.UnitTests.Domain;

public class WorkItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Now.AddHours(1);

    private static WorkItem Create(
        IssueType issueType = IssueType.Story,
        WorkItem? parent = null,
        Guid? projectId = null,
        string title = "Title",
        int number = 1,
        Guid? reporterId = null) =>
        new(Guid.NewGuid(), projectId ?? Guid.NewGuid(), issueType, title, number, reporterId ?? Guid.NewGuid(), parent, Now);

    // ---- Constructor guard clauses ----

    [Fact]
    public void Constructor_ValidArguments_StartsAtTodo()
    {
        var workItem = Create();

        Assert.Equal(WorkItemStatus.Todo, workItem.Status);
        Assert.Null(workItem.ParentId);
        Assert.Null(workItem.AssigneeId);
    }

    [Fact]
    public void Constructor_EmptyProjectId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => Create(projectId: Guid.Empty));

        Assert.Equal("projectId", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankTitle_ThrowsArgumentException(string invalidTitle)
    {
        var ex = Assert.Throws<ArgumentException>(() => Create(title: invalidTitle));

        Assert.Equal("title", ex.ParamName);
    }

    [Fact]
    public void Constructor_TitleTooLong_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => Create(title: new string('a', 201)));

        Assert.Equal("title", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NumberLessThanOne_ThrowsArgumentException(int invalidNumber)
    {
        var ex = Assert.Throws<ArgumentException>(() => Create(number: invalidNumber));

        Assert.Equal("number", ex.ParamName);
    }

    [Fact]
    public void Constructor_EmptyReporterId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => Create(reporterId: Guid.Empty));

        Assert.Equal("reporterId", ex.ParamName);
    }

    // ---- Rule 1: status transitions - exhaustive, all 16 (from, to) combinations ----

    [Theory]
    [InlineData(WorkItemStatus.Todo, WorkItemStatus.InProgress)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.InReview)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.Todo)]
    [InlineData(WorkItemStatus.InReview, WorkItemStatus.Done)]
    [InlineData(WorkItemStatus.InReview, WorkItemStatus.InProgress)]
    public void Transition_LegalMove_Succeeds(WorkItemStatus from, WorkItemStatus to)
    {
        var workItem = Create(IssueType.Story);
        MoveTo(workItem, from);

        workItem.Transition(to, Later);

        Assert.Equal(to, workItem.Status);
        Assert.Equal(Later, workItem.UpdatedAt);
    }

    [Theory]
    // self-transitions
    [InlineData(WorkItemStatus.Todo, WorkItemStatus.Todo)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.InProgress)]
    [InlineData(WorkItemStatus.InReview, WorkItemStatus.InReview)]
    [InlineData(WorkItemStatus.Done, WorkItemStatus.Done)]
    // skips work not yet started / not yet reviewed
    [InlineData(WorkItemStatus.Todo, WorkItemStatus.InReview)]
    [InlineData(WorkItemStatus.Todo, WorkItemStatus.Done)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.Done)]
    // backwards past what the machine allows
    [InlineData(WorkItemStatus.InReview, WorkItemStatus.Todo)]
    // Done is terminal
    [InlineData(WorkItemStatus.Done, WorkItemStatus.Todo)]
    [InlineData(WorkItemStatus.Done, WorkItemStatus.InProgress)]
    [InlineData(WorkItemStatus.Done, WorkItemStatus.InReview)]
    public void Transition_IllegalMove_ThrowsInvalidTransitionException(WorkItemStatus from, WorkItemStatus to)
    {
        var workItem = Create(IssueType.Story);
        MoveTo(workItem, from);

        var ex = Assert.Throws<InvalidTransitionException>(() => workItem.Transition(to, Later));

        Assert.Equal(from, ex.From);
        Assert.Equal(to, ex.To);
    }

    /// <summary>Drives a freshly-created (Todo) work item to <paramref name="status"/> via legal moves.</summary>
    private static void MoveTo(WorkItem workItem, WorkItemStatus status)
    {
        if (status == WorkItemStatus.Todo)
        {
            return;
        }

        workItem.Transition(WorkItemStatus.InProgress, Now);
        if (status == WorkItemStatus.InProgress)
        {
            return;
        }

        workItem.Transition(WorkItemStatus.InReview, Now);
        if (status == WorkItemStatus.InReview)
        {
            return;
        }

        workItem.Transition(WorkItemStatus.Done, Now);
    }

    // ---- Rule 2: hierarchy tiers ----

    [Fact]
    public void Constructor_EpicWithNoParent_Succeeds()
    {
        var epic = Create(IssueType.Epic);

        Assert.Null(epic.ParentId);
    }

    [Fact]
    public void Constructor_EpicWithParent_ThrowsInvalidHierarchyException()
    {
        var anotherEpic = Create(IssueType.Epic);

        var ex = Assert.Throws<InvalidHierarchyException>(() => Create(IssueType.Epic, anotherEpic));

        Assert.Equal(IssueType.Epic, ex.ChildType);
        Assert.Equal(IssueType.Epic, ex.ParentType);
    }

    [Theory]
    [InlineData(IssueType.Story)]
    [InlineData(IssueType.Task)]
    [InlineData(IssueType.Bug)]
    public void Constructor_StoryTaskOrBugWithNoParent_Succeeds(IssueType childType)
    {
        var workItem = Create(childType);

        Assert.Null(workItem.ParentId);
    }

    [Theory]
    [InlineData(IssueType.Story)]
    [InlineData(IssueType.Task)]
    [InlineData(IssueType.Bug)]
    public void Constructor_StoryTaskOrBugWithEpicParent_Succeeds(IssueType childType)
    {
        var epic = Create(IssueType.Epic);

        var workItem = Create(childType, epic);

        Assert.Equal(epic.Id, workItem.ParentId);
    }

    [Theory]
    [InlineData(IssueType.Story, IssueType.Story)]
    [InlineData(IssueType.Task, IssueType.Bug)]
    [InlineData(IssueType.Bug, IssueType.SubTask)]
    public void Constructor_StoryTaskOrBugWithNonEpicParent_ThrowsInvalidHierarchyException(
        IssueType childType, IssueType parentType)
    {
        var invalidParent = parentType == IssueType.SubTask
            ? Create(IssueType.SubTask, Create(IssueType.Task))
            : Create(parentType);

        var ex = Assert.Throws<InvalidHierarchyException>(() => Create(childType, invalidParent));

        Assert.Equal(childType, ex.ChildType);
        Assert.Equal(parentType, ex.ParentType);
    }

    [Fact]
    public void Constructor_SubTaskWithNoParent_ThrowsInvalidHierarchyException()
    {
        var ex = Assert.Throws<InvalidHierarchyException>(() => Create(IssueType.SubTask));

        Assert.Equal(IssueType.SubTask, ex.ChildType);
        Assert.Null(ex.ParentType);
    }

    [Theory]
    [InlineData(IssueType.Story)]
    [InlineData(IssueType.Task)]
    [InlineData(IssueType.Bug)]
    public void Constructor_SubTaskWithStoryTaskOrBugParent_Succeeds(IssueType parentType)
    {
        var parent = Create(parentType);

        var subTask = Create(IssueType.SubTask, parent);

        Assert.Equal(parent.Id, subTask.ParentId);
    }

    [Fact]
    public void Constructor_SubTaskWithEpicParent_ThrowsInvalidHierarchyException()
    {
        var epic = Create(IssueType.Epic);

        var ex = Assert.Throws<InvalidHierarchyException>(() => Create(IssueType.SubTask, epic));

        Assert.Equal(IssueType.SubTask, ex.ChildType);
        Assert.Equal(IssueType.Epic, ex.ParentType);
    }

    [Fact]
    public void Constructor_SubTaskWithSubTaskParent_ThrowsInvalidHierarchyException()
    {
        var parentSubTask = Create(IssueType.SubTask, Create(IssueType.Task));

        var ex = Assert.Throws<InvalidHierarchyException>(() => Create(IssueType.SubTask, parentSubTask));

        Assert.Equal(IssueType.SubTask, ex.ChildType);
        Assert.Equal(IssueType.SubTask, ex.ParentType);
    }
}
