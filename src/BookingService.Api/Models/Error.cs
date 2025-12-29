namespace BookingService.Api.Models;

public class Error
{
    public string Code { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? ResourceType { get; set; }
    public object? ResourceId { get; set; }
}