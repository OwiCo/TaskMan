using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Contracts;
using TaskFlow.Api.Domain.Entities;
using TaskFlow.Api.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests;

public class CommentsEndpointTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE comments, work_items, projects, users RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid WorkItemId, Guid AuthorId)> SeedWorkItemAndAuthorAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var now = timeProvider.GetUtcNow();

        var project = new Project(Guid.NewGuid(), "SED", "Seed Project", now);
        var user = new User(Guid.NewGuid(), "Seed Author", "author@example.com", now);
        db.Projects.Add(project);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var number = project.AllocateNextNumber(now);
        var workItem = new WorkItem(Guid.NewGuid(), project.Id, IssueType.Task, "Seed Task", number, user.Id, null, now);
        db.WorkItems.Add(workItem);
        await db.SaveChangesAsync();

        return (workItem.Id, user.Id);
    }

    [Fact]
    public async Task GetAll_CleanDatabase_ReturnsEmptyList()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var comments = await response.Content.ReadFromJsonAsync<List<CommentResponse>>();
        Assert.Empty(comments!);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201AndPersists()
    {
        var client = factory.CreateClient();
        var (workItemId, authorId) = await SeedWorkItemAndAuthorAsync();
        var request = new CreateCommentRequest(workItemId, authorId, "A real comment.");

        var response = await client.PostAsJsonAsync("/api/v1/comments", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CommentResponse>();
        Assert.Equal(workItemId, created!.WorkItemId);
        Assert.Equal(authorId, created.AuthorId);
        Assert.Equal("A real comment.", created.Body);

        var listResponse = await client.GetAsync("/api/v1/comments");
        var comments = await listResponse.Content.ReadFromJsonAsync<List<CommentResponse>>();
        Assert.Contains(comments!, c => c.Id == created.Id);
    }

    [Fact]
    public async Task Create_NonexistentWorkItem_Returns404()
    {
        var client = factory.CreateClient();
        var (_, authorId) = await SeedWorkItemAndAuthorAsync();
        var request = new CreateCommentRequest(Guid.NewGuid(), authorId, "Orphan comment.");

        var response = await client.PostAsJsonAsync("/api/v1/comments", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_NonexistentAuthor_Returns404()
    {
        var client = factory.CreateClient();
        var (workItemId, _) = await SeedWorkItemAndAuthorAsync();
        var request = new CreateCommentRequest(workItemId, Guid.NewGuid(), "Ghost author.");

        var response = await client.PostAsJsonAsync("/api/v1/comments", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_BlankBody_Returns400()
    {
        var client = factory.CreateClient();
        var (workItemId, authorId) = await SeedWorkItemAndAuthorAsync();
        var request = new CreateCommentRequest(workItemId, authorId, "");

        var response = await client.PostAsJsonAsync("/api/v1/comments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
