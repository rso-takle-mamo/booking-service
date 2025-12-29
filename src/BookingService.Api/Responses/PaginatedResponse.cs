namespace BookingService.Api.Responses;

public class PaginatedResponse<T> where T : class
{
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyList<T> Data { get; set; } = new List<T>();
}