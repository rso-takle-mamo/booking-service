namespace BookingService.Api.Exceptions;

public class ConflictException(string conflictType, string message) : BaseDomainException(message)
{
    public override string ErrorCode => "CONFLICT";
    
    public string ConflictType { get; } = conflictType;
}