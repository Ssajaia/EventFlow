using StackExchange.Redis;

namespace EventFlow.GatewayApi.Middleware;

/// <summary>
/// Redis sliding window rate limiter.
/// Limits requests per IP per minute.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly int _limit;
    private readonly TimeSpan _window;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IConnectionMultiplexer redis,
        ILogger<RateLimitingMiddleware> logger,
        IConfiguration config)
    {
        _next = next;
        _redis = redis;
        _logger = logger;
        _limit = int.Parse(config["RATE_LIMIT_REQUESTS"] ?? "100");
        _window = TimeSpan.FromSeconds(int.Parse(config["RATE_LIMIT_WINDOW_SECONDS"] ?? "60"));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"rate:{ip}:{DateTime.UtcNow:yyyyMMddHHmm}";
        var db = _redis.GetDatabase();

        var current = await db.StringIncrementAsync(key);
        if (current == 1)
            await db.KeyExpireAsync(key, _window);

        context.Response.Headers["X-RateLimit-Limit"] = _limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, _limit - (int)current).ToString();

        if (current > _limit)
        {
            _logger.LogWarning("Rate limit exceeded for IP {Ip}: {Count}/{Limit}", ip, current, _limit);
            context.Response.StatusCode = 429;
            await context.Response.WriteAsync("Too Many Requests");
            return;
        }

        await _next(context);
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
        context.Request.Headers["X-Correlation-Id"] = id;
        context.Response.Headers["X-Correlation-Id"] = id;
        await _next(context);
    }
}
