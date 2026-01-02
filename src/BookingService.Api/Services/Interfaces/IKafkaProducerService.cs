using BookingService.Api.Events;

namespace BookingService.Api.Services.Interfaces;

public interface IKafkaProducerService
{
    Task PublishBookingEventAsync(BaseEvent bookingEvent, CancellationToken cancellationToken = default);
}
