namespace BMS.API.Middleware;

/// <summary>
/// Logs every incoming HTTP request with method, path, and response status code.
/// Useful for auditing and debugging without touching controller code.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation(
            "HTTP {Method} {Path} started",
            context.Request.Method,
            context.Request.Path);

        await _next(context);

        _logger.LogInformation(
            "HTTP {Method} {Path} responded {StatusCode}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode);
    }
}
