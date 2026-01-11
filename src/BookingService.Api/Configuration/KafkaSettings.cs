namespace BookingService.Api.Configuration;

public class KafkaSettings
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string TenantEventsTopic { get; set; } = string.Empty;
    public string ServiceCatalogEventsTopic { get; set; } = string.Empty;
    public string BookingEventsTopic { get; set; } = string.Empty;
    public string ConsumerGroupId { get; set; } = string.Empty;
    public bool EnableAutoCommit { get; set; }
    public string AutoOffsetReset { get; set; } = "Earliest";

    public string Acks { get; set; } = "all";
    public bool EnableIdempotence { get; set; } = true;
    public int MessageTimeoutMs { get; set; } = 5000;
    public int RequestTimeoutMs { get; set; } = 3000;

    public string SecurityProtocol { get; set; } = "SaslSsl";
    public string SaslMechanism { get; set; } = "Plain";
    public string SaslUsername { get; set; } = "$ConnectionString";
    public string SaslPassword { get; set; } = string.Empty;
}
