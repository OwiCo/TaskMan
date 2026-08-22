using TaskFlow.Api.Application.Services;

namespace TaskFlow.Api.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ProjectService>();
        services.AddScoped<WorkItemService>();
        services.AddScoped<CommentService>();
        services.AddScoped<UserService>();

        return services;
    }
}
