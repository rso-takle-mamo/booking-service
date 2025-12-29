using System.ComponentModel.DataAnnotations;

namespace BookingService.Api.Requests;

public class PaginationRequest
{
    /// <summary>
    /// Number of items to skip (for pagination). Optional, defaults to 0.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Offset must be greater than or equal to 0")]
    public int Offset { get; set; } = 0;

    /// <summary>
    /// Number of items to return. Optional, defaults to 100.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Limit must be between 1 and 100")]
    public int Limit { get; set; } = 100;
}