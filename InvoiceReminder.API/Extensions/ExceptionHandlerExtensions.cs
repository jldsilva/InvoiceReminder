using InvoiceReminder.API.Exceptions;
using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics.CodeAnalysis;

namespace InvoiceReminder.API.Extensions;

[ExcludeFromCodeCoverage]
public static class ExceptionHandlerExtensions
{
    public static IServiceCollection AddExceptionHandler(this IServiceCollection services)
    {
        _ = services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            {
                var activity = context.HttpContext.Features.Get<IHttpActivityFeature>().Activity;

                context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
                context.ProblemDetails.Extensions.Add("requestId", context.HttpContext.TraceIdentifier);
                context.ProblemDetails.Extensions.Add("activityId", activity?.Id);
            });

        _ = services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
