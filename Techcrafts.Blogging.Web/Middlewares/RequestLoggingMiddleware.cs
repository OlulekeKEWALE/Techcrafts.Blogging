using Techcrafts.Blogging.Application.Interfaces;

namespace Techcrafts.Blogging.Web.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAnalyticsBufferService analyticsBuffer)
    {
        // Only track GET requests that are not static files, APIs, or Admin
        if (context.Request.Method == "GET" && 
            !context.Request.Path.StartsWithSegments("/css") && 
            !context.Request.Path.StartsWithSegments("/js") && 
            !context.Request.Path.StartsWithSegments("/lib") &&
            !context.Request.Path.StartsWithSegments("/account") &&
            !context.Request.Path.StartsWithSegments("/admin"))
        {
            var url = context.Request.Path.ToString();
            
            // Note: Post reads are already captured, but we will capture them here globally if we parse them out,
            // or just capture them as generic page views. The analytics queries can distinguish by path.
            analyticsBuffer.EnqueuePageView(null, context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers["User-Agent"], url);
        }
        
        await _next(context);
    }
}
