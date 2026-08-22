using TaskFlow.Api.Domain.Entities;

namespace TaskFlow.UnitTests.Domain;

public class ProjectTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_ValidArguments_SetsInitialState()
    {
        var id = Guid.NewGuid();

        var project = new Project(id, "ENG", "Engineering", Now);

        Assert.Equal(id, project.Id);
        Assert.Equal("ENG", project.Key);
        Assert.Equal("Engineering", project.Name);
        Assert.Equal(1, project.NextItemNumber);
        Assert.Equal(Now, project.CreatedAt);
        Assert.Equal(Now, project.UpdatedAt);
    }

    [Theory]
    [InlineData("E")]              // too short
    [InlineData("ENGINEERING1")]   // too long, and contains a digit
    [InlineData("eng")]            // lowercase
    [InlineData("EN-G")]           // contains a hyphen
    [InlineData("")]               // blank
    public void Constructor_InvalidKey_ThrowsArgumentException(string invalidKey)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new Project(Guid.NewGuid(), invalidKey, "Engineering", Now));

        Assert.Equal("key", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankName_ThrowsArgumentException(string invalidName)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new Project(Guid.NewGuid(), "ENG", invalidName, Now));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Constructor_NameTooLong_ThrowsArgumentException()
    {
        var tooLong = new string('a', 201);

        var ex = Assert.Throws<ArgumentException>(
            () => new Project(Guid.NewGuid(), "ENG", tooLong, Now));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Constructor_NameAtMaxLength_Succeeds()
    {
        var maxLength = new string('a', 200);

        var project = new Project(Guid.NewGuid(), "ENG", maxLength, Now);

        Assert.Equal(maxLength, project.Name);
    }

    [Fact]
    public void AllocateNextNumber_FirstCall_ReturnsOneAndIncrements()
    {
        var project = new Project(Guid.NewGuid(), "ENG", "Engineering", Now);
        var later = Now.AddHours(1);

        var number = project.AllocateNextNumber(later);

        Assert.Equal(1, number);
        Assert.Equal(2, project.NextItemNumber);
        Assert.Equal(later, project.UpdatedAt);
    }

    [Fact]
    public void AllocateNextNumber_CalledTwice_ReturnsSequentialNumbers()
    {
        var project = new Project(Guid.NewGuid(), "ENG", "Engineering", Now);

        var first = project.AllocateNextNumber(Now);
        var second = project.AllocateNextNumber(Now);

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(3, project.NextItemNumber);
    }
}
