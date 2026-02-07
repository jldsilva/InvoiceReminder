using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceReminder.API.Endpoints;

public class InvoiceEndpoints : IEndpointDefinition
{
    private const string basepath = "/api/invoice";

    public void RegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        var endpoint = endpoints.MapGroup(basepath).WithName("InvoiceEndpoints");

        MapGetInvoices(endpoint);
        MapGetInvoice(endpoint);
        MapGetInvoiceByBarcode(endpoint);
        MapCreateInvoice(endpoint);
        MapUpdateInvoice(endpoint);
        MapDeleteInvoice(endpoint);
    }

    private static void MapGetInvoices(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/", (IInvoiceAppService appService) =>
            {
                var result = appService.GetAll();

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("GetInvoices")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapGetInvoice(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/{id}", async (IInvoiceAppService appService, Guid id, CancellationToken ct) =>
            {
                var result = await appService.GetByIdAsync(id, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetInvoice")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapGetInvoiceByBarcode(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/getby-barcode/{value}",
            async (IInvoiceAppService appService, string value, CancellationToken ct) =>
            {
                var result = await appService.GetByBarcodeAsync(value, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetInvoiceByBarcode")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapCreateInvoice(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPost("/",
            async (IInvoiceAppService appService, [FromBody] InvoiceViewModel viewModel, CancellationToken ct) =>
            {
                var result = await appService.AddAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.Created($"{basepath}/{result.Value.Barcode}", result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("CreateInvoice")
            .RequireAuthorization()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapUpdateInvoice(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPut("/",
            async (IInvoiceAppService appService, [FromBody] InvoiceViewModel viewModel, CancellationToken ct) =>
            {
                var result = await appService.UpdateAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("UpdateInvoice")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapDeleteInvoice(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapDelete("/",
            async (IInvoiceAppService appService, [FromBody] InvoiceViewModel viewModel, CancellationToken ct) =>
            {
                var result = await appService.RemoveAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.Problem(result.Error);
            })
            .WithName("DeleteInvoice")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
