using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceReminder.API.Endpoints;

public class InvoiceEndpoints : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        var endpoint = endpoints.MapGroup("/api/invoice").WithName("InvoiceEndpoints");

        _ = endpoint.MapGet("/", (IInvoiceAppService invoiceAppService) =>
            {
                var result = invoiceAppService.GetAll();

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("GetInvoices")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapGet("/{id}", async (IInvoiceAppService invoiceAppService, CancellationToken ct, Guid id) =>
            {
                var result = await invoiceAppService.GetByIdAsync(id, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetInvoice")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapGet("/get_by_barcode/{value}",
            async (IInvoiceAppService invoiceAppService, CancellationToken ct, string value) =>
            {
                var result = await invoiceAppService.GetByBarcodeAsync(value, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetInvoiceByBarCode")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapPost("/",
            async (IInvoiceAppService invoiceAppService, CancellationToken ct, [FromBody] InvoiceViewModel invoiceViewModel) =>
            {
                var result = await invoiceAppService.AddAsync(invoiceViewModel, ct);

                return result.IsSuccess
                    ? Results.Created($"/api/invoice/{result.Value.Barcode}", result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("CreateInvoice")
            .RequireAuthorization()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapPut("/",
            async (IInvoiceAppService invoiceAppService, CancellationToken ct, [FromBody] InvoiceViewModel invoiceViewModel) =>
            {
                var result = await invoiceAppService.UpdateAsync(invoiceViewModel, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("UpdateInvoice")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapDelete("/",
            async (IInvoiceAppService invoiceAppService, CancellationToken ct, [FromBody] InvoiceViewModel invoiceViewModel) =>
            {
                var result = await invoiceAppService.RemoveAsync(invoiceViewModel, ct);

                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.Problem(result.Error);
            })
            .WithName("DeleteInvoice")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
