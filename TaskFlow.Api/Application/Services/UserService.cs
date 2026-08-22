using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Domain.Entities;
using TaskFlow.Api.Infrastructure.Persistence;

namespace TaskFlow.Api.Application.Services;

public sealed class UserService(AppDbContext db, TimeProvider timeProvider)
{
    public async Task<User> CreateAsync(string name, string email, CancellationToken cancellationToken)
    {
        var user = new User(Guid.NewGuid(), name, email, timeProvider.GetUtcNow());

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .ToListAsync(cancellationToken);
}
