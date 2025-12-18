using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceReminder.API.Endpoints;

public class ScanEmailDefinitionEndpoints : IEndpointDefinition
{
    private const string basepath = "/api/scan_email";

    public void RegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        var endpoint = endpoints.MapGroup(basepath).WithName("ScanEmailDefinitionEndpoints");

        MapGetScanEmailDefinitions(endpoint);
        MapGetScanEmailDefinition(endpoint);
        MapGetByUserId(endpoint);
        MapGetBySenderEmailAddress(endpoint);
        MapCreateScanEmailDefinition(endpoint);
        MapUpdateScanEmailDefinition(endpoint);
        MapDeleteScanEmailDefinition(endpoint);
    }

    private static void MapGetScanEmailDefinitions(RouteGroupBuilder endpoint)
    {
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
    }

    private static void MapGetScanEmailDefinition(RouteGroupBuilder endpoint)
    {
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
    }

    private static void MapGetByUserId(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/getby-userid/{id}",
            async (IScanEmailDefinitionAppService scanEmailDefinitionAppService, CancellationToken ct, Guid id) =>
            {
                var result = await scanEmailDefinitionAppService.GetByUserIdAsync(id, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetByUserId")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapGetBySenderEmailAddress(RouteGroupBuilder endpoint)
    {
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
    }

    private static void MapCreateScanEmailDefinition(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPost("/",
            async (IScanEmailDefinitionAppService scanEmailDefinitionAppService,
                CancellationToken ct,
                [FromBody] ScanEmailDefinitionViewModel scanEmailDefinitionViewModel) =>
            {
                var result = await scanEmailDefinitionAppService.AddAsync(scanEmailDefinitionViewModel, ct);

                return result.IsSuccess
                    ? Results.Created($"{basepath}/{result.Value.Id}", result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("CreateScanEmailDefinition")
            .RequireAuthorization()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapUpdateScanEmailDefinition(RouteGroupBuilder endpoint)
    {
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
    }

    private static void MapDeleteScanEmailDefinition(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapDelete("/",
            async (IScanEmailDefinitionAppService scanEmailDefinitionAppService, CancellationToken ct,
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
