using System.Text.Json;
using System.Text.Json.Nodes;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BookingService.Api.Configuration;
using BookingService.Api.Events.Tenant;
using BookingService.Api.Events.Service;
using BookingService.Api.Events.Category;
using BookingService.Api.Services.Interfaces;

namespace BookingService.Api.Services;

public class KafkaConsumerService(
    ILogger<KafkaConsumerService> logger,
    IServiceProvider serviceProvider,
    IOptions<KafkaSettings> kafkaSettings)
    : BackgroundService
{
    private readonly KafkaSettings _kafkaSettings = kafkaSettings.Value;

    private ConsumerConfig CreateTenantConsumerConfig()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            GroupId = $"{_kafkaSettings.ConsumerGroupId}-tenant-events",
            AutoOffsetReset = KafkaConstants.ParseAutoOffsetReset(_kafkaSettings.AutoOffsetReset),
            EnableAutoCommit = _kafkaSettings.EnableAutoCommit
        };

        if (string.IsNullOrEmpty(_kafkaSettings.SaslPassword)) return config;
        config.SecurityProtocol = ParseSecurityProtocol(_kafkaSettings.SecurityProtocol);
        config.SaslMechanism = ParseSaslMechanism(_kafkaSettings.SaslMechanism);
        config.SaslUsername = _kafkaSettings.SaslUsername;
        config.SaslPassword = _kafkaSettings.SaslPassword;

        return config;
    }

    private ConsumerConfig CreateServiceCatalogConsumerConfig()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            GroupId = $"{_kafkaSettings.ConsumerGroupId}-service-catalog-events",
            AutoOffsetReset = KafkaConstants.ParseAutoOffsetReset(_kafkaSettings.AutoOffsetReset),
            EnableAutoCommit = _kafkaSettings.EnableAutoCommit
        };

        if (string.IsNullOrEmpty(_kafkaSettings.SaslPassword)) return config;
        config.SecurityProtocol = ParseSecurityProtocol(_kafkaSettings.SecurityProtocol);
        config.SaslMechanism = ParseSaslMechanism(_kafkaSettings.SaslMechanism);
        config.SaslUsername = _kafkaSettings.SaslUsername;
        config.SaslPassword = _kafkaSettings.SaslPassword;

        return config;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Kafka Consumer Service starting...");

        _ = Task.Run(async () =>
        {
            try
            {
                await ConsumeTenantEventsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tenant events consumer failed unexpectedly.");
            }
        }, cancellationToken);

        _ = Task.Run(async () =>
        {
            try
            {
                await ConsumeServiceCatalogEventsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Service catalog events consumer failed unexpectedly.");
            }
        }, cancellationToken);

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private async Task ConsumeTenantEventsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting tenant events consumer for topic: {Topic}", _kafkaSettings.TenantEventsTopic);

        var consumerConfig = CreateTenantConsumerConfig();

        IConsumer<Ignore, string>? consumer = null;
        var retryCount = 0;
        var retryDelayMs = 2000;

        while (consumer == null && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
                consumer.Subscribe(_kafkaSettings.TenantEventsTopic);
                logger.LogInformation("Successfully connected to Kafka and subscribed to topic: {Topic}", _kafkaSettings.TenantEventsTopic);
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                logger.LogWarning(ex, "Failed to initialize Kafka consumer (attempt {RetryCount}). Retrying in {Delay}ms...", retryCount, retryDelayMs);
                consumer?.Dispose();
                consumer = null;
                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(retryDelayMs, cancellationToken);
                }
            }
        }

        if (consumer == null)
        {
            logger.LogError("Failed to initialize Kafka consumer after cancellation. Stopping consumer.");
            return;
        }

        try
        {
            using (consumer)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(cancellationToken);
                    if (consumeResult.Message is null) continue;

                    logger.LogInformation("Received tenant event: {EventType}", consumeResult.Message.Key);

                    try
                    {
                        var jsonNode = JsonNode.Parse(consumeResult.Message.Value);
                        var eventType = jsonNode?["eventType"]?.GetValue<string>();
                        if (string.IsNullOrEmpty(eventType))
                        {
                            logger.LogWarning("Event missing eventType field");
                            continue;
                        }

                        await ProcessTenantEventAsync(eventType, consumeResult.Message.Value);
                        consumer.Commit(consumeResult);
                    }
                    catch (JsonException jsonEx)
                    {
                        logger.LogError(jsonEx, "Failed to parse tenant event JSON: {Message}", consumeResult.Message.Value);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing tenant event: {EventType}", consumeResult.Message.Key);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Tenant events consumer stopped due to cancellation");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tenant events consumer error");
        }
    }

    private async Task ConsumeServiceCatalogEventsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting service catalog events consumer for topic: {Topic}", _kafkaSettings.ServiceCatalogEventsTopic);

        var consumerConfig = CreateServiceCatalogConsumerConfig();

        IConsumer<Ignore, string>? consumer = null;
        var retryCount = 0;
        var retryDelayMs = 2000;

        while (consumer == null && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
                consumer.Subscribe(_kafkaSettings.ServiceCatalogEventsTopic);
                logger.LogInformation("Successfully connected to Kafka and subscribed to topic: {Topic}", _kafkaSettings.ServiceCatalogEventsTopic);
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                logger.LogWarning(ex, "Failed to initialize Kafka consumer (attempt {RetryCount}). Retrying in {Delay}ms...", retryCount, retryDelayMs);
                consumer?.Dispose();
                consumer = null;
                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(retryDelayMs, cancellationToken);
                }
            }
        }

        if (consumer == null)
        {
            logger.LogError("Failed to initialize Kafka consumer after cancellation. Stopping consumer.");
            return;
        }

        try
        {
            using (consumer)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(cancellationToken);
                    if (consumeResult.Message is null) continue;

                    logger.LogInformation("Received service catalog event: {EventType}", consumeResult.Message.Key);

                    try
                    {
                        var jsonNode = JsonNode.Parse(consumeResult.Message.Value);
                        var eventType = jsonNode?["eventType"]?.GetValue<string>();
                        if (string.IsNullOrEmpty(eventType))
                        {
                            logger.LogWarning("Event missing eventType field");
                            continue;
                        }

                        await ProcessServiceCatalogEventAsync(eventType, consumeResult.Message.Value);
                        consumer.Commit(consumeResult);
                    }
                    catch (JsonException jsonEx)
                    {
                        logger.LogError(jsonEx, "Failed to parse service catalog event JSON: {Message}", consumeResult.Message.Value);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing service catalog event: {EventType}", consumeResult.Message.Key);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Service catalog events consumer stopped due to cancellation");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Service catalog events consumer error");
        }
    }

    private async Task ProcessTenantEventAsync(string eventType, string eventJson)
    {
        using var scope = serviceProvider.CreateScope();
        var tenantEventService = scope.ServiceProvider.GetRequiredService<ITenantEventService>();

        try
        {
            switch (eventType)
            {
                case nameof(TenantCreatedEvent):
                    var tenantCreatedEvent = JsonSerializer.Deserialize<TenantCreatedEvent>(eventJson, KafkaConstants.JsonSerializerOptions);
                    if (tenantCreatedEvent != null)
                    {
                        logger.LogInformation("Processing TenantCreatedEvent for tenant {TenantId}", tenantCreatedEvent.TenantId);
                        await tenantEventService.HandleTenantCreatedEventAsync(tenantCreatedEvent);
                    }
                    break;

                case nameof(TenantUpdatedEvent):
                    var tenantUpdatedEvent = JsonSerializer.Deserialize<TenantUpdatedEvent>(eventJson, KafkaConstants.JsonSerializerOptions);
                    if (tenantUpdatedEvent != null)
                    {
                        logger.LogInformation("Processing TenantUpdatedEvent for tenant {TenantId}", tenantUpdatedEvent.TenantId);
                        await tenantEventService.HandleTenantUpdatedEventAsync(tenantUpdatedEvent);
                    }
                    break;

                default:
                    logger.LogWarning("Unknown tenant event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing tenant event {EventType}", eventType);
            throw;
        }
    }

    private async Task ProcessServiceCatalogEventAsync(string eventType, string eventJson)
    {
        using var scope = serviceProvider.CreateScope();
        var serviceCatalogEventService = scope.ServiceProvider.GetRequiredService<IServiceCatalogEventService>();

        try
        {
            switch (eventType)
            {
                case nameof(ServiceCreatedEvent):
                    var serviceCreatedEvent = JsonSerializer.Deserialize<ServiceCreatedEvent>(eventJson, KafkaConstants.JsonSerializerOptions);
                    if (serviceCreatedEvent != null)
                    {
                        logger.LogInformation("Processing ServiceCreatedEvent for service {ServiceId}", serviceCreatedEvent.ServiceId);
                        await serviceCatalogEventService.HandleServiceCreatedEventAsync(serviceCreatedEvent);
                    }
                    break;

                case nameof(ServiceEditedEvent):
                    var serviceEditedEvent = JsonSerializer.Deserialize<ServiceEditedEvent>(eventJson, KafkaConstants.JsonSerializerOptions);
                    if (serviceEditedEvent != null)
                    {
                        logger.LogInformation("Processing ServiceEditedEvent for service {ServiceId}", serviceEditedEvent.ServiceId);
                        await serviceCatalogEventService.HandleServiceEditedEventAsync(serviceEditedEvent);
                    }
                    break;

                case nameof(ServiceDeletedEvent):
                    var serviceDeletedEvent = JsonSerializer.Deserialize<ServiceDeletedEvent>(eventJson, KafkaConstants.JsonSerializerOptions);
                    if (serviceDeletedEvent != null)
                    {
                        logger.LogInformation("Processing ServiceDeletedEvent for service {ServiceId}", serviceDeletedEvent.ServiceId);
                        await serviceCatalogEventService.HandleServiceDeletedEventAsync(serviceDeletedEvent);
                    }
                    break;

                case nameof(CategoryCreatedEvent):
                    var categoryCreatedEvent = JsonSerializer.Deserialize<CategoryCreatedEvent>(eventJson, KafkaConstants.JsonSerializerOptions);
                    if (categoryCreatedEvent != null)
                    {
                        logger.LogInformation("Processing CategoryCreatedEvent for category {CategoryId}", categoryCreatedEvent.CategoryId);
                        await serviceCatalogEventService.HandleCategoryCreatedEventAsync(categoryCreatedEvent);
                    }
                    break;

                case nameof(CategoryEditedEvent):
                    var categoryEditedEvent = JsonSerializer.Deserialize<CategoryEditedEvent>(eventJson, KafkaConstants.JsonSerializerOptions);
                    if (categoryEditedEvent != null)
                    {
                        logger.LogInformation("Processing CategoryEditedEvent for category {CategoryId}", categoryEditedEvent.CategoryId);
                        await serviceCatalogEventService.HandleCategoryEditedEventAsync(categoryEditedEvent);
                    }
                    break;

                case nameof(CategoryDeletedEvent):
                    var categoryDeletedEvent = JsonSerializer.Deserialize<CategoryDeletedEvent>(eventJson, KafkaConstants.JsonSerializerOptions);
                    if (categoryDeletedEvent != null)
                    {
                        logger.LogInformation("Processing CategoryDeletedEvent for category {CategoryId}", categoryDeletedEvent.CategoryId);
                        await serviceCatalogEventService.HandleCategoryDeletedEventAsync(categoryDeletedEvent);
                    }
                    break;

                default:
                    logger.LogWarning("Unknown service catalog event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing service catalog event {EventType}", eventType);
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Kafka Consumer Service stopping...");
        await base.StopAsync(cancellationToken);
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
