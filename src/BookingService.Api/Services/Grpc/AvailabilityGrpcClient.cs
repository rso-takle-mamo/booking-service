using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using Google.Protobuf.WellKnownTypes;
using BookingService.Api.Configuration;
using BookingService.Api.Exceptions;

namespace BookingService.Api.Services.Grpc;

public class AvailabilityGrpcClient : IAvailabilityGrpcClient, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly Availability.AvailabilityService.AvailabilityServiceClient _client;
    private readonly ILogger<AvailabilityGrpcClient> _logger;

    public AvailabilityGrpcClient(
        IOptions<AvailabilityServiceGrpcSettings> settings,
        ILogger<AvailabilityGrpcClient> logger)
    {
        _logger = logger;
        var settings1 = settings.Value;

        _channel = GrpcChannel.ForAddress(settings1.Url, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
                PooledConnectionLifetime = TimeSpan.FromMinutes(15)
            },
            Credentials = ChannelCredentials.Insecure
        });

        _client = new Availability.AvailabilityService.AvailabilityServiceClient(_channel);

        _logger.LogInformation("Availability gRPC client initialized with URL: {GrpcUrl}", settings1.Url);
    }

    public async Task<TimeSlotResponse> CheckAvailabilityAsync(TimeSlotRequest request)
    {
        _logger.LogInformation("Checking availability for tenant {TenantId}, service {ServiceId}, time {StartTime}-{EndTime}",
            request.TenantId, request.ServiceId, request.StartTime, request.EndTime);

        try
        {
            var grpcRequest = new Availability.TimeSlotRequest
            {
                TenantId = request.TenantId,
                ServiceId = request.ServiceId,
                StartTime = Timestamp.FromDateTime(request.StartTime.ToUniversalTime()),
                EndTime = Timestamp.FromDateTime(request.EndTime.ToUniversalTime())
            };

            var response = await _client.CheckTimeSlotAvailabilityAsync(grpcRequest,
                deadline: DateTime.UtcNow.AddSeconds(5));

            _logger.LogInformation("Availability check completed. Available: {IsAvailable}", response.IsAvailable);

            if (!response.IsAvailable)
            {
                _logger.LogWarning("Time slot not available. Conflicts: {ConflictCount}", response.Conflicts.Count);
            }

            var result = new TimeSlotResponse
            {
                IsAvailable = response.IsAvailable
            };

            foreach (var conflict in response.Conflicts)
            {
                result.Conflicts.Add(new ConflictInfo
                {
                    Type = MapConflictType(conflict.Type),
                    OverlapStart = conflict.OverlapStart.ToDateTime(),
                    OverlapEnd = conflict.OverlapEnd.ToDateTime()
                });
            }

            return result;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            _logger.LogError(ex, "Availability service request timed out");
            throw new ServiceUnavailableException("Availability service is temporarily unavailable. Please try again later.");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            _logger.LogError(ex, "Availability service is unavailable");
            throw new ServiceUnavailableException("Availability service is currently unavailable. Please try again later.");
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Error calling availability service. Status: {StatusCode}", ex.StatusCode);
            throw new ServiceUnavailableException("Error communicating with availability service. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking availability");
            throw new ServiceUnavailableException("An unexpected error occurred while checking availability. Please try again later.");
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }

    private static ConflictType MapConflictType(Availability.ConflictType type)
    {
        return type switch
        {
            Availability.ConflictType.TimeBlock => ConflictType.TimeBlock,
            Availability.ConflictType.WorkingHours => ConflictType.WorkingHours,
            Availability.ConflictType.Booking => ConflictType.Booking,
            Availability.ConflictType.BufferTime => ConflictType.BufferTime,
            _ => ConflictType.Unspecified
        };
    }
}