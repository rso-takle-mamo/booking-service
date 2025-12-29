namespace BookingService.Api.Exceptions;

public class DatabaseOperationException(string operation, string entity, string message, Exception innerException)
    : BaseDomainException(message, innerException)
{
    public override string ErrorCode => "DATABASE_ERROR";
    
    public string Operation { get; } = operation;

    public string Entity { get; } = entity;
}