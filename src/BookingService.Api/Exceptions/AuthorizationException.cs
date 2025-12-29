namespace BookingService.Api.Exceptions;

public class AuthorizationException(string resource, string action, string message) : BaseDomainException(message)
{
    public override string ErrorCode => "ACCESS_DENIED";
    
    public string Resource { get; } = resource;

    public string Action { get; } = action;
}