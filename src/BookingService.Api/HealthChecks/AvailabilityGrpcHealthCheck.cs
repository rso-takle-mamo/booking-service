using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using BookingService.Api.Configuration;
using BookingService.Api.Services.Grpc;

namespace BookingService.Api.HealthChecks;

public sealed class AvailabilityGrpcHealthCheck(
    IServiceProvider serviceProvider,
    IOptions<AvailabilityServiceGrpcSettings> grpcSettings,
    ILogger<AvailabilityGrpcHealthCheck> logger)
    : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = grpcSettings.Value.Url;

            if (string.IsNullOrWhiteSpace(url))
            {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy("gRPC URL not configured"));
            }

            return Task.FromResult(!Uri.TryCreate(url, UriKind.Absolute, out _) ? HealthCheckResult.Unhealthy($"gRPC URL is invalid: {url}") : HealthCheckResult.Healthy($"gRPC client configured for: {url}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Availability gRPC health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Availability service not available", ex));
        }
    }
}
