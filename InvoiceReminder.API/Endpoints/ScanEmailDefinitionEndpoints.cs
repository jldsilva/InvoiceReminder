using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceReminder.API.Endpoints;

public class ScanEmailDefinitionEndpoints : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        var endpoint = endpoints.MapGroup("/api/scan_email").WithName("ScanEmailDefinitionEndpoints");

        _ = endpoint.MapGet("/", (IScanEmailDefinitionAppService scanEmailDefinitionAppService) =>
            {
                var result = scanEmailDefinitionAppService.GetAll();

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("GetScanEmailDefinitions")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapGet("/{id}",
            async (IScanEmailDefinitionAppService scanEmailDefinitionAppService, CancellationToken ct, Guid id) =>
            {
                var result = await scanEmailDefinitionAppService.GetByIdAsync(id, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetScanEmailDefinition")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapGet("/get_by_user_id/{id}",
            async (IScanEmailDefinitionAppService scanEmailDefinitionAppService, CancellationToken ct, Guid id) =>
            {
                var result = await scanEmailDefinitionAppService.GetByUserIdAsync(id);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetByUserId")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapGet("/{email}/{id}",
            async (IScanEmailDefinitionAppService scanEmailDefinitionAppService,
                CancellationToken ct,
                string email,
                Guid id) =>
            {
                var result = await scanEmailDefinitionAppService.GetBySenderEmailAddressAsync(email, id, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetBySenderEmailAddress")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapPost("/",
            async (IScanEmailDefinitionAppService scanEmailDefinitionAppService,
                CancellationToken ct,
                [FromBody] ScanEmailDefinitionViewModel scanEmailDefinitionViewModel) =>
            {
                var result = await scanEmailDefinitionAppService.AddAsync(scanEmailDefinitionViewModel, ct);

                return result.IsSuccess
                    ? Results.Created($"/api/scan-email-definition/{result.Value.Id}", result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("CreateScanEmailDefinition")
            .RequireAuthorization()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapPut("/",
            async (IScanEmailDefinitionAppService scanEmailDefinitionAppService,
                CancellationToken ct,
                [FromBody] ScanEmailDefinitionViewModel scanEmailDefinitionViewModel) =>
            {
                var result = await scanEmailDefinitionAppService.UpdateAsync(scanEmailDefinitionViewModel, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("UpdateScanEmailDefinition")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapDelete("/",
            async (IScanEmailDefinitionAppService scanEmailDefinitionAppService,
                CancellationToken ct,
                [FromBody] ScanEmailDefinitionViewModel scanEmailDefinitionViewModel) =>
            {
                var result = await scanEmailDefinitionAppService.RemoveAsync(scanEmailDefinitionViewModel, ct);

                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.Problem(result.Error);
            })
            .WithName("DeleteScanEmailDefinition")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
