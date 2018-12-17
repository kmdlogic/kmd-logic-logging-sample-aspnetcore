using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

// NOTE: purposely in no namespace to keep log noise to a minimum

/// <summary>
/// Enriches the Serilog <see cref="LogContext" /> with "UserIdentityName" pulled
/// from <see cref="HttpContext.User" /> on each request, only if 
/// <see cref="System.Security.Principal.IIdentity.IsAuthenticated" /> is <c>true</c>.
/// </summary>
public class SerilogUserContextMiddleware
{
    private readonly RequestDelegate _next;
    public SerilogUserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var identity = httpContext?.User?.Identity;
        var username = identity?.Name;
        if (identity?.IsAuthenticated == true)
        {
            using (LogContext.PushProperty("UserIdentityName", username))
            {
                await _next(httpContext);
            }
        }
        else
        {
            await _next(httpContext);
        }
    }
}