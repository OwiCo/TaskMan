using TaskFlow.Api.Domain.Entities;

namespace TaskFlow.UnitTests.Domain;

public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static User Create(string name = "Ada Lovelace", string email = "ada@example.com") =>
        new(Guid.NewGuid(), name, email, Now);

    [Fact]
    public void Constructor_ValidArguments_SetsFields()
    {
        var id = Guid.NewGuid();

        var user = new User(id, "Ada Lovelace", "ada@example.com", Now);

        Assert.Equal(id, user.Id);
        Assert.Equal("Ada Lovelace", user.Name);
        Assert.Equal("ada@example.com", user.Email);
        Assert.Equal(Now, user.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankName_ThrowsArgumentException(string invalidName)
    {
        var ex = Assert.Throws<ArgumentException>(() => Create(name: invalidName));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Constructor_NameTooLong_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => Create(name: new string('a', 201)));

        Assert.Equal("name", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankEmail_ThrowsArgumentException(string invalidEmail)
    {
        var ex = Assert.Throws<ArgumentException>(() => Create(email: invalidEmail));

        Assert.Equal("email", ex.ParamName);
    }

    [Fact]
    public void Constructor_EmailTooLong_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => Create(email: new string('a', 201)));

        Assert.Equal("email", ex.ParamName);
    }
}
