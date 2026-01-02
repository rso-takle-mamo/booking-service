using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;

namespace BookingService.Api.Configuration;

public static class KafkaConstants
{
    public static JsonSerializerOptions JsonSerializerOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Confluent.Kafka.AutoOffsetReset ParseAutoOffsetReset(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "earliest" => Confluent.Kafka.AutoOffsetReset.Earliest,
            "latest" => Confluent.Kafka.AutoOffsetReset.Latest,
            "none" or "error" => Confluent.Kafka.AutoOffsetReset.Error,
            _ => Confluent.Kafka.AutoOffsetReset.Earliest
        };
    }

    public static Acks? ParseAcks(string acksValue)
    {
        return acksValue.ToLowerInvariant() switch
        {
            "all" => Acks.All,
            "0" => Acks.None,
            "1" => Acks.Leader,
            _ => Acks.All // Default
        };
    }
}
