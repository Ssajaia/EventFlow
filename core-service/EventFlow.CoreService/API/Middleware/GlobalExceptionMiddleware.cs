using EventFlow.SharedKernel.DTOs;
using System.Net;
using System.Text.Json;

namespace EventFlow.CoreService.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            context.Response.ContentType = "application/json";
            var (statusCode, message) = ex switch
            {
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ex.Message),
                InvalidOperationException => (HttpStatusCode.BadRequest, ex.Message),
                KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
                ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
            };
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(
                ApiResponse<object>.Fail(message),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    }
}

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = id;
        context.Response.Headers["X-Correlation-Id"] = id;
        await _next(context);
    }
}
