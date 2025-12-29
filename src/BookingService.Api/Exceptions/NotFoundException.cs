namespace BookingService.Api.Exceptions;

public class NotFoundException : BaseDomainException
{
    public override string ErrorCode => "NOT_FOUND";
    
    public string ResourceType { get; }

    public object ResourceId { get; }

    public NotFoundException(string resourceType, object resourceId)
        : base($"{resourceType} with ID '{resourceId}' was not found.")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public NotFoundException(string resourceType, object resourceId, Exception innerException)
        : base($"{resourceType} with ID '{resourceId}' was not found.", innerException)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}