namespace EventFlow.CoreService.API.Extensions;

public static class HttpContextExtensions
{
    public static string GetCorrelationId(this HttpContext context)
    {
        if (context.Items.TryGetValue("CorrelationId", out var id) && id is string correlationId)
            return correlationId;

        return context.Request.Headers.TryGetValue("X-Correlation-Id", out var header)
            ? header.ToString()
            : Guid.NewGuid().ToString();
    }
}
