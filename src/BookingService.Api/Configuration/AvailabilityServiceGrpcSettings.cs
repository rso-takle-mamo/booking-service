namespace BookingService.Api.Configuration;

public class AvailabilityServiceGrpcSettings
{
    private string _url = string.Empty;

    public string Url
    {
        get => _url;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("AvailabilityService gRPC URL is required", nameof(Url));
            _url = value;
        }
    }
}