using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Text.Json;
using BookingService.Api.Configuration;
using BookingService.Api.Events;
using BookingService.Api.Services.Interfaces;

namespace BookingService.Api.Services;

public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly KafkaSettings _kafkaSettings;
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(
        IOptions<KafkaSettings> kafkaSettings,
        ILogger<KafkaProducerService> logger)
    {
        _kafkaSettings = kafkaSettings.Value;
        _logger = logger;

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            ClientId = _kafkaSettings.ClientId,
            Acks = KafkaConstants.ParseAcks(_kafkaSettings.Acks),
            EnableIdempotence = _kafkaSettings.EnableIdempotence,
            MessageTimeoutMs = _kafkaSettings.MessageTimeoutMs,
            RequestTimeoutMs = _kafkaSettings.RequestTimeoutMs
        };

        if (!string.IsNullOrEmpty(_kafkaSettings.SaslPassword))
        {
            producerConfig.SecurityProtocol = ParseSecurityProtocol(_kafkaSettings.SecurityProtocol);
            producerConfig.SaslMechanism = ParseSaslMechanism(_kafkaSettings.SaslMechanism);
            producerConfig.SaslUsername = _kafkaSettings.SaslUsername;
            producerConfig.SaslPassword = _kafkaSettings.SaslPassword;
        }

        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
    }

    public async Task PublishBookingEventAsync(BaseEvent bookingEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new Message<Null, string>
            {
                Value = JsonSerializer.Serialize(bookingEvent, bookingEvent.GetType(), KafkaConstants.JsonSerializerOptions)
            };

            var deliveryResult = await _producer.ProduceAsync(
                _kafkaSettings.BookingEventsTopic,
                message,
                cancellationToken);

            var entityId = bookingEvent switch
            {
                Events.Booking.BookingEvent bookingEvt => bookingEvt.BookingId.ToString(),
                _ => "N/A"
            };

            _logger.LogInformation(
                "Booking event {EventType} published to topic {Topic} partition {Partition} at offset {Offset} [EventId: {EventId}, EntityId: {EntityId}]",
                bookingEvent.EventType,
                _kafkaSettings.BookingEventsTopic,
                deliveryResult.Partition,
                deliveryResult.Offset,
                bookingEvent.EventId,
                entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish booking event {EventType} [EventId: {EventId}]", bookingEvent.EventType, bookingEvent.EventId);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
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
