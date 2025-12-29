namespace BookingService.Api.Models;

public class ErrorResponse
{
    public required Error Error { get; set; }
}

public class ValidationErrorResponse : ErrorResponse
{
    public required List<ValidationError> ValidationErrors { get; set; }
}

public static class ErrorResponses
{
    public static ErrorResponse Create(string code, string message)
    {
        return new ErrorResponse
        {
            Error = new Error
            {
                Code = code,
                Message = message
            }
        };
    }

    public static ErrorResponse Create(string code, string message, string resourceType, object? resourceId)
    {
        return new ErrorResponse
        {
            Error = new Error
            {
                Code = code,
                Message = message,
                ResourceType = resourceType,
                ResourceId = resourceId
            }
        };
    }

    public static ValidationErrorResponse CreateValidation(string message, List<ValidationError>? validationErrors)
    {
        // If no validation errors provided, create a single error from the message
        if (validationErrors == null || validationErrors.Count == 0)
        {
            validationErrors =
            [
                new ValidationError()
                {
                    Field = "General",
                    Message = message
                }
            ];
        }

        // If we have field-specific errors, don't include the message in the error object
        return new ValidationErrorResponse
        {
            Error = new Error
            {
                Code = "VALIDATION_ERROR"
            },
            ValidationErrors = validationErrors
        };
    }
}