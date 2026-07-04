using System.Diagnostics;
using System.Security.Claims;

namespace InvoiceReminder.API.Middleware;

internal sealed class UserContextMiddlerware(RequestDelegate next, ILogger<UserContextMiddlerware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier);

        if (userId is not null)
        {
            _ = (Activity.Current?.SetTag("user.id", userId));

            var data = new Dictionary<string, object>
            {
                ["UserId"] = userId
            };

            using (logger.BeginScope(data))
            {
                await next(context);
            }
        }
        else
        {
            await next(context);
        }
    }
}
