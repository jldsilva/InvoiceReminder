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
        _ = endpoint.MapGet("/", (IScanEmailDefinitionAppService appService) =>
            {
                var result = appService.GetAll();

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("GetScanEmailDefinitions")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapGetScanEmailDefinition(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/{id}",
            async (IScanEmailDefinitionAppService appService, Guid id, CancellationToken ct) =>
            {
                var result = await appService.GetByIdAsync(id, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetScanEmailDefinition")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapGetByUserId(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/getby-userid/{id}",
            async (IScanEmailDefinitionAppService appService, Guid id, CancellationToken ct) =>
            {
                var result = await appService.GetByUserIdAsync(id, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetByUserId")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapGetBySenderEmailAddress(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/getby-sender/{email}/{id}",
            async (IScanEmailDefinitionAppService appService, string email, Guid id, CancellationToken ct) =>
            {
                var result = await appService.GetBySenderEmailAddressAsync(email, id, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetBySenderEmailAddress")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapCreateScanEmailDefinition(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPost("/",
            async (IScanEmailDefinitionAppService appService, [FromBody] ScanEmailDefinitionViewModel viewModel,
            CancellationToken ct) =>
            {
                var result = await appService.AddAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.Created($"{basepath}/{result.Value.Id}", result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("CreateScanEmailDefinition")
            .RequireAuthorization()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapUpdateScanEmailDefinition(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPut("/",
            async (IScanEmailDefinitionAppService appService, [FromBody] ScanEmailDefinitionViewModel viewModel,
            CancellationToken ct) =>
            {
                var result = await appService.UpdateAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("UpdateScanEmailDefinition")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapDeleteScanEmailDefinition(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapDelete("/",
            async (IScanEmailDefinitionAppService appService, [FromBody] ScanEmailDefinitionViewModel viewModel,
            CancellationToken ct) =>
            {
                var result = await appService.RemoveAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.Problem(result.Error);
            })
            .WithName("DeleteScanEmailDefinition")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
