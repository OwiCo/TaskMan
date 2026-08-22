using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using TaskFlow.Api.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests;

public sealed class TaskFlowApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("taskflow")
        .WithUsername("taskflow")
        .WithPassword("taskflow_test_only")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Program.cs reads the connection string eagerly (once, synchronously) to build
        // AppDbContext's options before this factory's config override reliably lands in the
        // merged configuration - relying on config timing here isn't safe. Replacing the
        // DbContextOptions registration directly is the documented, reliable way to redirect
        // an already-configured DbContext to a different connection string in tests.
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options
                .UseNpgsql(_postgres.GetConnectionString())
                .UseSnakeCaseNamingConvention());
        });
    }

    public Task InitializeAsync() => _postgres.StartAsync();

    public new Task DisposeAsync() => _postgres.DisposeAsync().AsTask();
}
