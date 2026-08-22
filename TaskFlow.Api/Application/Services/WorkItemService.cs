using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Domain.Entities;
using TaskFlow.Api.Domain.Exceptions;
using TaskFlow.Api.Infrastructure.Persistence;

namespace TaskFlow.Api.Application.Services;

public sealed class WorkItemService(AppDbContext db, TimeProvider timeProvider)
{
    public async Task<WorkItem> CreateAsync(
        Guid projectId,
        IssueType issueType,
        string title,
        Guid reporterId,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken)
            ?? throw new NotFoundException($"Project '{projectId}' was not found.");

        WorkItem? parent = null;
        if (parentId is not null)
        {
            parent = await db.WorkItems.FirstOrDefaultAsync(w => w.Id == parentId, cancellationToken)
                ?? throw new NotFoundException($"Work item '{parentId}' was not found.");
        }

        var now = timeProvider.GetUtcNow();
        var number = project.AllocateNextNumber(now);

        var workItem = new WorkItem(Guid.NewGuid(), projectId, issueType, title, number, reporterId, parent, now);

        db.WorkItems.Add(workItem);
        await db.SaveChangesAsync(cancellationToken);

        return workItem;
    }

    public async Task<WorkItem> TransitionAsync(
        Guid id, WorkItemStatus to, CancellationToken cancellationToken)
    {
        var workItem = await db.WorkItems.FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Work item '{id}' was not found.");

        workItem.Transition(to, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        return workItem;
    }

    public async Task<List<WorkItem>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.WorkItems
            .AsNoTracking()
            .OrderBy(w => w.ProjectId)
            .ThenBy(w => w.Number)
            .ToListAsync(cancellationToken);
}
