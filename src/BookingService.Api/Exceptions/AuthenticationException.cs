namespace BookingService.Api.Exceptions;


public class AuthenticationException(string authenticationType, string message) : BaseDomainException(message)
{
    public override string ErrorCode => "AUTHENTICATION_FAILED";
    
    public string AuthenticationType { get; } = authenticationType;
}