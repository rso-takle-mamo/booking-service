using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;

namespace BookingService.Api.Services.Grpc;

public interface IAvailabilityGrpcClient
{
    Task<TimeSlotResponse> CheckAvailabilityAsync(TimeSlotRequest request);
}

public class TimeSlotRequest
{
    public string TenantId { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class TimeSlotResponse
{
    public bool IsAvailable { get; set; }
    public List<ConflictInfo> Conflicts { get; set; } = new();
}

public class ConflictInfo
{
    public ConflictType Type { get; set; }
    public DateTime OverlapStart { get; set; }
    public DateTime OverlapEnd { get; set; }
}

public enum ConflictType
{
    Unspecified,
    TimeBlock,
    WorkingHours,
    Booking,
    BufferTime
}