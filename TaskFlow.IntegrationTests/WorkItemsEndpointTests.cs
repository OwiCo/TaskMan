using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Contracts;
using TaskFlow.Api.Domain.Entities;
using TaskFlow.Api.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests;

/// <summary>
/// Same shared-container pattern as ProjectsEndpointTests: truncate before each test since xUnit
/// doesn't guarantee method order. Project/User are seeded directly via AppDbContext rather than
/// through their own endpoints - they're fixtures here, not the thing under test.
/// </summary>
public class WorkItemsEndpointTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>, IAsyncLifetime
{
    // The app's Program.cs registers JsonStringEnumConverter for its own serialization, but that
    // doesn't affect this test's HttpClient, which has its own default JSON options - without this,
    // reading IssueType/Status back out of a response fails since the client doesn't know "Epic" is
    // a valid enum representation.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
    };

    public async Task InitializeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE comments, work_items, projects, users RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid ProjectId, Guid ReporterId)> SeedProjectAndReporterAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var project = new Project(Guid.NewGuid(), "SED", "Seed Project", timeProvider.GetUtcNow());
        var user = new User(Guid.NewGuid(), "Seed User", "seed@example.com", timeProvider.GetUtcNow());

        db.Projects.Add(project);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return (project.Id, user.Id);
    }

    [Fact]
    public async Task GetAll_CleanDatabase_ReturnsEmptyList()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/workitems");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var workItems = await response.Content.ReadFromJsonAsync<List<WorkItemResponse>>(JsonOptions);
        Assert.Empty(workItems!);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithNumberOneAndTodoStatus()
    {
        var client = factory.CreateClient();
        var (projectId, reporterId) = await SeedProjectAndReporterAsync();
        var request = new CreateWorkItemRequest(projectId, IssueType.Epic, "An Epic", reporterId, null);

        var response = await client.PostAsJsonAsync("/api/v1/workitems", request, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<WorkItemResponse>(JsonOptions);
        Assert.Equal(1, created!.Number);
        Assert.Equal(WorkItemStatus.Todo, created.Status);
        Assert.Null(created.ParentId);
    }

    [Fact]
    public async Task Create_UnderExistingParent_SetsParentId()
    {
        var client = factory.CreateClient();
        var (projectId, reporterId) = await SeedProjectAndReporterAsync();
        var epicResponse = await client.PostAsJsonAsync(
            "/api/v1/workitems", new CreateWorkItemRequest(projectId, IssueType.Epic, "Parent Epic", reporterId, null), JsonOptions);
        var epic = await epicResponse.Content.ReadFromJsonAsync<WorkItemResponse>(JsonOptions);

        var storyResponse = await client.PostAsJsonAsync(
            "/api/v1/workitems", new CreateWorkItemRequest(projectId, IssueType.Story, "Child Story", reporterId, epic!.Id), JsonOptions);

        Assert.Equal(HttpStatusCode.Created, storyResponse.StatusCode);
        var story = await storyResponse.Content.ReadFromJsonAsync<WorkItemResponse>(JsonOptions);
        Assert.Equal(epic.Id, story!.ParentId);
        Assert.Equal(2, story.Number);
    }

    [Fact]
    public async Task Create_SubTaskDirectlyUnderEpic_Returns409()
    {
        var client = factory.CreateClient();
        var (projectId, reporterId) = await SeedProjectAndReporterAsync();
        var epicResponse = await client.PostAsJsonAsync(
            "/api/v1/workitems", new CreateWorkItemRequest(projectId, IssueType.Epic, "Parent Epic", reporterId, null), JsonOptions);
        var epic = await epicResponse.Content.ReadFromJsonAsync<WorkItemResponse>(JsonOptions);

        var response = await client.PostAsJsonAsync(
            "/api/v1/workitems", new CreateWorkItemRequest(projectId, IssueType.SubTask, "Bad Subtask", reporterId, epic!.Id), JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_NonexistentProject_Returns404()
    {
        var client = factory.CreateClient();
        var (_, reporterId) = await SeedProjectAndReporterAsync();
        var request = new CreateWorkItemRequest(Guid.NewGuid(), IssueType.Task, "Orphan", reporterId, null);

        var response = await client.PostAsJsonAsync("/api/v1/workitems", request, JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_NonexistentReporter_Returns404()
    {
        var client = factory.CreateClient();
        var (projectId, _) = await SeedProjectAndReporterAsync();
        var request = new CreateWorkItemRequest(projectId, IssueType.Task, "No Reporter", Guid.NewGuid(), null);

        var response = await client.PostAsJsonAsync("/api/v1/workitems", request, JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Transition_LegalMove_Returns200AndUpdatesStatus()
    {
        var client = factory.CreateClient();
        var (projectId, reporterId) = await SeedProjectAndReporterAsync();
        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/workitems", new CreateWorkItemRequest(projectId, IssueType.Task, "Task", reporterId, null), JsonOptions);
        var workItem = await createResponse.Content.ReadFromJsonAsync<WorkItemResponse>(JsonOptions);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/workitems/{workItem!.Id}/transitions", new TransitionWorkItemRequest(WorkItemStatus.InProgress), JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<WorkItemResponse>(JsonOptions);
        Assert.Equal(WorkItemStatus.InProgress, updated!.Status);
        Assert.True(updated.UpdatedAt > updated.CreatedAt);
    }

    [Fact]
    public async Task Transition_IllegalMove_Returns409()
    {
        var client = factory.CreateClient();
        var (projectId, reporterId) = await SeedProjectAndReporterAsync();
        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/workitems", new CreateWorkItemRequest(projectId, IssueType.Task, "Task", reporterId, null), JsonOptions);
        var workItem = await createResponse.Content.ReadFromJsonAsync<WorkItemResponse>(JsonOptions);

        // Todo -> Done is not a legal transition (has to pass through InProgress/InReview first).
        var response = await client.PostAsJsonAsync(
            $"/api/v1/workitems/{workItem!.Id}/transitions", new TransitionWorkItemRequest(WorkItemStatus.Done), JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Transition_NonexistentWorkItem_Returns404()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/workitems/{Guid.NewGuid()}/transitions", new TransitionWorkItemRequest(WorkItemStatus.InProgress), JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
