namespace BookingService.Api.Exceptions;

public class ServiceUnavailableException(string message) : Exception(message)
{
    public string ErrorCode { get; } = "SERVICE_UNAVAILABLE";
}