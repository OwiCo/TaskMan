using TaskFlow.Api.Domain.Entities;

namespace TaskFlow.UnitTests.Domain;

public class CommentTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Comment Create(Guid? workItemId = null, Guid? authorId = null, string body = "A comment.") =>
        new(Guid.NewGuid(), workItemId ?? Guid.NewGuid(), authorId ?? Guid.NewGuid(), body, Now);

    [Fact]
    public void Constructor_ValidArguments_SetsFields()
    {
        var id = Guid.NewGuid();
        var workItemId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        var comment = new Comment(id, workItemId, authorId, "A comment.", Now);

        Assert.Equal(id, comment.Id);
        Assert.Equal(workItemId, comment.WorkItemId);
        Assert.Equal(authorId, comment.AuthorId);
        Assert.Equal("A comment.", comment.Body);
        Assert.Equal(Now, comment.CreatedAt);
    }

    [Fact]
    public void Constructor_EmptyWorkItemId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => Create(workItemId: Guid.Empty));

        Assert.Equal("workItemId", ex.ParamName);
    }

    [Fact]
    public void Constructor_EmptyAuthorId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => Create(authorId: Guid.Empty));

        Assert.Equal("authorId", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankBody_ThrowsArgumentException(string invalidBody)
    {
        var ex = Assert.Throws<ArgumentException>(() => Create(body: invalidBody));

        Assert.Equal("body", ex.ParamName);
    }

    [Fact]
    public void Constructor_BodyTooLong_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => Create(body: new string('a', 2001)));

        Assert.Equal("body", ex.ParamName);
    }

    [Fact]
    public void Constructor_BodyAtMaxLength_Succeeds()
    {
        var maxLength = new string('a', 2000);

        var comment = Create(body: maxLength);

        Assert.Equal(maxLength, comment.Body);
    }
}
