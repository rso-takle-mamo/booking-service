namespace BookingService.Api.Exceptions;

public abstract class BaseDomainException : Exception
{
    public abstract string ErrorCode { get; }

    protected BaseDomainException(string message) : base(message) { }

    protected BaseDomainException(string message, Exception innerException) : base(message, innerException) { }
}