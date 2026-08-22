using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

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

        // This override now works reliably because AddInfrastructure() reads the connection string
        // lazily (once per AppDbContext construction via the IServiceProvider-aware AddDbContext
        // overload), not eagerly at startup - see DependencyInjection.cs. Capturing the connection
        // string in a closure at startup was the actual bug; this config override was never the
        // problem.
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("ConnectionStrings:Postgres", _postgres.GetConnectionString())
            ]);
        });
    }

    public Task InitializeAsync() => _postgres.StartAsync();

    public new Task DisposeAsync() => _postgres.DisposeAsync().AsTask();
}
