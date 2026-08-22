using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Domain.Entities;
using TaskFlow.Api.Domain.Exceptions;
using TaskFlow.Api.Infrastructure.Persistence;

namespace TaskFlow.Api.Application.Services;

public sealed class ProjectService(AppDbContext db, TimeProvider timeProvider)
{
    public async Task<Project> CreateAsync(string key, string name, CancellationToken cancellationToken)
    {
        var keyTaken = await db.Projects.AnyAsync(p => p.Key == key, cancellationToken);
        if (keyTaken)
        {
            throw new DuplicateKeyException($"A project with key '{key}' already exists.", key);
        }

        var project = new Project(Guid.NewGuid(), key, name, timeProvider.GetUtcNow());

        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        return project;
    }

    public async Task<List<Project>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.Projects
            .AsNoTracking()
            .OrderBy(p => p.Key)
            .ToListAsync(cancellationToken);
}
