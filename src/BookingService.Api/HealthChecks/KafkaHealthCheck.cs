using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using BookingService.Api.Configuration;

namespace BookingService.Api.HealthChecks;

public sealed class KafkaHealthCheck(
    IOptions<KafkaSettings> kafkaSettings,
    ILogger<KafkaHealthCheck> logger)
    : IHealthCheck
{
    private readonly KafkaSettings _kafkaSettings = kafkaSettings.Value;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = new AdminClientConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers,
                SocketTimeoutMs = 5000
            };

            if (!string.IsNullOrEmpty(_kafkaSettings.SaslPassword))
            {
                config.SecurityProtocol = ParseSecurityProtocol(_kafkaSettings.SecurityProtocol);
                config.SaslMechanism = ParseSaslMechanism(_kafkaSettings.SaslMechanism);
                config.SaslUsername = _kafkaSettings.SaslUsername;
                config.SaslPassword = _kafkaSettings.SaslPassword;
            }

            using var adminClient = new AdminClientBuilder(config).Build();

            adminClient.GetMetadata(TimeSpan.FromSeconds(5));

            return Task.FromResult(
                HealthCheckResult.Healthy("Kafka broker reachable"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kafka health check failed");

            return Task.FromResult(
                HealthCheckResult.Unhealthy("Kafka broker not reachable", ex));
        }
    }

    private static SecurityProtocol ParseSecurityProtocol(string protocol)
        => Enum.TryParse<SecurityProtocol>(protocol, out var parsed)
            ? parsed
            : SecurityProtocol.SaslSsl;

    private static SaslMechanism ParseSaslMechanism(string mechanism)
        => Enum.TryParse<SaslMechanism>(mechanism, out var parsed)
            ? parsed
            : SaslMechanism.Plain;
}
