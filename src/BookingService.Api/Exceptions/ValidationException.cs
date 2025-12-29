using BookingService.Api.Models;

namespace BookingService.Api.Exceptions;

public class ValidationException : BaseDomainException
{
    public override string ErrorCode => "VALIDATION_ERROR";
    
    public List<ValidationError> ValidationErrors { get; }

    public ValidationException(List<ValidationError> validationErrors)
        : base($"Validation failed with {validationErrors.Count} error(s).")
    {
        ValidationErrors = validationErrors;
    }

    public ValidationException(string message) : base(message)
    {
        ValidationErrors = [];
    }
}