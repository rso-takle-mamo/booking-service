using System.Diagnostics;
using System.Linq;
using System.Security.Claims;

namespace BookingService.Api.Middleware;

public class RequestResponseLoggingMiddleware(ILogger<RequestResponseLoggingMiddleware> logger) : IMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const long SlowRequestThresholdMs = 1000;

    private static readonly string[] SkippedPaths = ["/health", "/health/live", "/health/ready", "/metrics"];

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip logging for health checks and metrics
        if (SkippedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var correlationId = GetOrCreateCorrelationId(context);
        var stopwatch = Stopwatch.StartNew();

        LogRequest(context, correlationId);

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();

            LogResponse(context, correlationId, stopwatch.ElapsedMilliseconds);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdValue))
        {
            var correlationId = correlationIdValue.FirstOrDefault();
            if (!string.IsNullOrEmpty(correlationId))
            {
                context.TraceIdentifier = correlationId;
                return correlationId;
            }
        }

        var newCorrelationId = Guid.NewGuid().ToString();
        context.TraceIdentifier = newCorrelationId;
        context.Response.Headers[CorrelationIdHeaderName] = newCorrelationId;

        return newCorrelationId;
    }

    private void LogRequest(HttpContext context, string correlationId)
    {
        var userId = GetUserId(context);
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        logger.LogInformation(
            "HTTP Request started " +
            "{CorrelationId} " +
            "{UserId} " +
            "{RequestMethod} " +
            "{RequestPath} " +
            "{UserAgent} " +
            "{RemoteIp}",
            correlationId,
            userId,
            requestMethod,
            requestPath,
            userAgent ?? "Unknown",
            forwardedFor?.Split(',')[0].Trim() ?? context.Connection.RemoteIpAddress?.ToString()
        );
    }

    private void LogResponse(HttpContext context, string correlationId, long elapsedMilliseconds)
    {
        var statusCode = context.Response.StatusCode;
        var userId = GetUserId(context);
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        switch (statusCode)
        {
            case >= 500:
                logger.LogError(
                    "{CorrelationId} " +
                    "{UserId} " +
                    "{RequestMethod} " +
                    "{RequestPath} " +
                    "{StatusCode} " +
                    "{DurationMs}",
                    correlationId,
                    userId,
                    requestMethod,
                    requestPath,
                    statusCode,
                    elapsedMilliseconds
                );
                break;
            case >= 400:
                logger.LogWarning(
                    "HTTP Request failed with client error " +
                    "{CorrelationId} " +
                    "{UserId} " +
                    "{RequestMethod} " +
                    "{RequestPath} " +
                    "{StatusCode} " +
                    "{DurationMs}",
                    correlationId,
                    userId,
                    requestMethod,
                    requestPath,
                    statusCode,
                    elapsedMilliseconds
                );
                break;
            default:
            {
                if (elapsedMilliseconds > SlowRequestThresholdMs)
                {
                    logger.LogWarning(
                        "HTTP Request completed slowly " +
                        "{CorrelationId} " +
                        "{UserId} " +
                        "{RequestMethod} " +
                        "{RequestPath} " +
                        "{StatusCode} " +
                        "{DurationMs}",
                        correlationId,
                        userId,
                        requestMethod,
                        requestPath,
                        statusCode,
                        elapsedMilliseconds
                    );
                }
                else
                {
                    logger.LogInformation(
                        "HTTP Request completed successfully " +
                        "{CorrelationId} " +
                        "{UserId} " +
                        "{RequestMethod} " +
                        "{RequestPath} " +
                        "{StatusCode} " +
                        "{DurationMs}",
                        correlationId,
                        userId,
                        requestMethod,
                        requestPath,
                        statusCode,
                        elapsedMilliseconds
                    );
                }

                break;
            }
        }
    }

    private static string? GetUserId(HttpContext context)
    {
        return context.User.Identity?.IsAuthenticated == true ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value : null;
    }
}
