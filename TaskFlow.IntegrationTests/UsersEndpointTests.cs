using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Contracts;
using TaskFlow.Api.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests;

public class UsersEndpointTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE users RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAll_CleanDatabase_ReturnsEmptyList()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<UserResponse>>();
        Assert.Empty(users!);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201AndPersists()
    {
        var client = factory.CreateClient();
        var request = new CreateUserRequest("Jane Doe", "jane@example.com");

        var response = await client.PostAsJsonAsync("/api/v1/users", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("Jane Doe", created!.Name);
        Assert.Equal("jane@example.com", created.Email);

        var listResponse = await client.GetAsync("/api/v1/users");
        var users = await listResponse.Content.ReadFromJsonAsync<List<UserResponse>>();
        Assert.Contains(users!, u => u.Email == "jane@example.com");
    }

    [Fact]
    public async Task Create_BlankName_Returns400()
    {
        var client = factory.CreateClient();
        var request = new CreateUserRequest("", "jane@example.com");

        var response = await client.PostAsJsonAsync("/api/v1/users", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_BlankEmail_Returns400()
    {
        var client = factory.CreateClient();
        var request = new CreateUserRequest("Jane Doe", "");

        var response = await client.PostAsJsonAsync("/api/v1/users", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
