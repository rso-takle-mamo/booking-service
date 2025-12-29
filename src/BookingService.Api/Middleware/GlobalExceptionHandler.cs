using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using BookingService.Api.Models;
using BookingService.Api.Exceptions;

namespace BookingService.Api.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var errorResponse = CreateErrorResponse(exception);
            var statusCode = GetStatusCode(exception);

            logger.LogError(
                "Exception: {ExceptionType}, Message: {ExceptionMessage}, StatusCode: {StatusCode}",
                exception.GetType().Name,
                exception.Message,
                statusCode);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var responseJson = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(responseJson);
        }
    }

    private static object CreateErrorResponse(Exception exception)
    {
        return exception switch
        {
            ValidationException ex => ErrorResponses.CreateValidation(
                ex.Message,
                ex.ValidationErrors
            ),
            NotFoundException ex => ErrorResponses.Create(
                ex.ErrorCode,
                ex.Message,
                ex.ResourceType,
                ex.ResourceId
            ),
            ConflictException ex => ErrorResponses.Create(ex.ErrorCode, ex.Message),
            AuthenticationException ex => ErrorResponses.Create(ex.ErrorCode, ex.Message),
            AuthorizationException ex => ErrorResponses.Create(ex.ErrorCode, ex.Message),
            DatabaseOperationException ex => ErrorResponses.Create(ex.ErrorCode, ex.Message),

            DbUpdateException ex => ErrorResponses.Create(
                "DATABASE_ERROR",
                ex.InnerException?.Message ?? ex.Message
            ),

            _ => ErrorResponses.Create("INTERNAL_SERVER_ERROR", "An internal server error occurred.")
        };
    }

    private static int GetStatusCode(Exception exception) =>
        exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            AuthenticationException => StatusCodes.Status401Unauthorized,
            AuthorizationException => StatusCodes.Status403Forbidden,
            DatabaseOperationException => StatusCodes.Status500InternalServerError,
            DbUpdateException => StatusCodes.Status400BadRequest,

            _ => StatusCodes.Status500InternalServerError
        };
}