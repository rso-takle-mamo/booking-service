namespace BookingService.Api.Exceptions;

public class AlreadyExistsException : BaseDomainException
{
    public override string ErrorCode => "ALREADY_EXISTS";

    public string ResourceType { get; }
    public Guid? ExistingResourceId { get; }

    public AlreadyExistsException(string message) : base(message) { }

    public AlreadyExistsException(string resourceType, string message) : base(message)
    {
        ResourceType = resourceType;
    }

    public AlreadyExistsException(string resourceType, Guid existingResourceId, string message) : base(message)
    {
        ResourceType = resourceType;
        ExistingResourceId = existingResourceId;
    }

    public AlreadyExistsException(string message, Exception innerException) : base(message, innerException) { }

    public AlreadyExistsException(string resourceType, Guid existingResourceId, string message, Exception innerException) : base(message, innerException)
    {
        ResourceType = resourceType;
        ExistingResourceId = existingResourceId;
    }
}