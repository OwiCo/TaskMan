using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Contracts;
using TaskFlow.Api.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests;

/// <summary>
/// All tests in this class share one Postgres container (via IClassFixture), so each test truncates
/// the tables it touches before running - otherwise test order would silently matter, since xUnit
/// doesn't guarantee method execution order within a class.
/// </summary>
public class ProjectsEndpointTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE projects RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAll_CleanDatabase_ReturnsEmptyList()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var projects = await response.Content.ReadFromJsonAsync<List<ProjectResponse>>();
        Assert.Empty(projects!);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201AndPersists()
    {
        var client = factory.CreateClient();
        var request = new CreateProjectRequest("CRT", "Create Test");

        var response = await client.PostAsJsonAsync("/api/v1/projects", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.Equal("CRT", created!.Key);
        Assert.Equal("Create Test", created.Name);
        Assert.Equal(1, created.NextItemNumber);

        var listResponse = await client.GetAsync("/api/v1/projects");
        var projects = await listResponse.Content.ReadFromJsonAsync<List<ProjectResponse>>();
        Assert.Contains(projects!, p => p.Key == "CRT");
    }

    [Fact]
    public async Task Create_DuplicateKey_Returns409()
    {
        var client = factory.CreateClient();
        var request = new CreateProjectRequest("DUP", "Duplicate Test");
        await client.PostAsJsonAsync("/api/v1/projects", request);

        var response = await client.PostAsJsonAsync("/api/v1/projects", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidKeyFormat_Returns400()
    {
        var client = factory.CreateClient();
        var request = new CreateProjectRequest("bad-key", "Invalid Key Test");

        var response = await client.PostAsJsonAsync("/api/v1/projects", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
