using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Infrastructure.Persistence;

namespace TaskFlow.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Connection string is read lazily, per DbContext construction (once per scope/request), not
        // captured once at startup - by the time any AppDbContext is actually built, the app is fully
        // running and IConfiguration reflects every override, including a test host's. This is what
        // makes WebApplicationFactory's ConfigureAppConfiguration override actually work; capturing the
        // connection string eagerly at startup was the root cause of a real bug - see CLAUDE.md.
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Postgres' configuration value.");

            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        });

        return services;
    }
}
