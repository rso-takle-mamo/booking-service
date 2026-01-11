namespace BookingService.Api.Configuration;

public class CorsSettings
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [
        "http://localhost:3000",
        "https://appointments-booking-rso-dev.vercel.app",
        "https://appointments-booking-rso.vercel.app"
    ];
}
