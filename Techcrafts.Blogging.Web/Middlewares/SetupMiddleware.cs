using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Techcrafts.Blogging.Infrastructure.Persistence;
using Serilog;

namespace Techcrafts.Blogging.Web.Middlewares;

public class SetupMiddleware
{
    private readonly RequestDelegate _next;
    private static bool _isDatabaseConfigured = false;
    private static bool _hasCheckedDatabase = false;

    public SetupMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, BloggingDbContext dbContext)
    {
        // Allow static files to pass through
        if (context.Request.Path.StartsWithSegments("/css") || 
            context.Request.Path.StartsWithSegments("/js") || 
            context.Request.Path.StartsWithSegments("/lib") ||
            context.Request.Path.StartsWithSegments("/images"))
        {
            await _next(context);
            return;
        }

        // Allow the setup routes themselves to pass through
        if (context.Request.Path.StartsWithSegments("/Setup"))
        {
            await _next(context);
            return;
        }

        if (!_isDatabaseConfigured)
        {
            if (!_hasCheckedDatabase)
            {
                try
                {
                    _isDatabaseConfigured = await dbContext.Database.CanConnectAsync(context.RequestAborted);
                    if (_isDatabaseConfigured)
                    {
                        _hasCheckedDatabase = true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "SetupMiddleware: Could not connect to database.");
                    _isDatabaseConfigured = false;
                }
            }

            if (!_isDatabaseConfigured)
            {
                context.Response.Redirect("/Setup");
                return;
            }
        }

        await _next(context);
    }

    // Call this from SetupController after successfully configuring the database
    public static void ResetState()
    {
        _hasCheckedDatabase = false;
        _isDatabaseConfigured = false;
    }
}
