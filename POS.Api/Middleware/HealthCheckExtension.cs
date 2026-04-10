using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace POS.Api.Middleware;

public static class HealthCheckExtension
{
    private static readonly string[] DatabaseTags = { "database" };

    public static IServiceCollection AddHealthCheck(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new Exception("Connection string 'DefaultConnection' is not configured.");
        }

        services.AddHealthChecks()
            .AddNpgSql(
                connectionString,
                name: "postgresql",
                failureStatus: HealthStatus.Unhealthy,
                tags: DatabaseTags);

        services.AddHealthChecksUI(setupSettings =>
            {
                setupSettings.SetEvaluationTimeInSeconds(5);
                setupSettings.MaximumHistoryEntriesPerEndpoint(50);
                setupSettings.AddHealthCheckEndpoint("Product Microservice health check", "/health");
            })
            .AddInMemoryStorage();

        return services;
    }
}
