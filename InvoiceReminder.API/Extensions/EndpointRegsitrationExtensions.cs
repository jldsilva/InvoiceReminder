using InvoiceReminder.API.Endpoints;
using System.Diagnostics.CodeAnalysis;

namespace InvoiceReminder.API.Extensions;

[ExcludeFromCodeCoverage]
public static class EndpointRegsitrationExtensions
{
    public static void RegisterEndpoints(this IEndpointRouteBuilder endpointRouter)
    {
        var services = new ServiceCollection();

        _ = services.Scan(scan =>
            scan.FromAssemblyOf<IEndpointDefinition>()
                .AddClasses(classes => classes.AssignableTo<IEndpointDefinition>())
                .AsImplementedInterfaces()
        );

        var endpoints = services
            .BuildServiceProvider()
            .GetRequiredService<IEnumerable<IEndpointDefinition>>();

        foreach (var endpoint in endpoints)
        {
            endpoint.RegisterEndpoints(endpointRouter);
        }
    }
}
