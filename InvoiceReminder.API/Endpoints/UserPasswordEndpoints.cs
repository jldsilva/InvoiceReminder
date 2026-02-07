using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceReminder.API.Endpoints;

public class UserPasswordEndpoints : IEndpointDefinition
{
    private const string basepath = "/api/user_password";

    public void RegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        var endpoint = endpoints.MapGroup(basepath).WithName("UserPasswordEndpoints");

        MapCreateUserPassword(endpoint);
        MapCreateUsersPassword(endpoint);
        MapChangeUserPassword(endpoint);
        MapDeleteUserPassword(endpoint);
        MapUpdateUserPassword(endpoint);
    }

    private static void MapCreateUserPassword(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPost("/",
            async (IUserPasswordAppService appService, UserPasswordViewModel viewModel, CancellationToken ct) =>
            {
                var result = await appService.AddAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.Created($"/api/user_password/{result.Value.Id}", result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("CreateUserPassword")
            .RequireAuthorization()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapCreateUsersPassword(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPost("/bulk-insert",
            async (IUserPasswordAppService appService, ICollection<UserPasswordViewModel> viewModelCollection,
            CancellationToken ct) =>
            {
                var result = await appService.BulkInsertAsync(viewModelCollection, ct);

                return result.IsSuccess
                    ? Results.Created($"/api/user_password/bulk-insert", result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("BulkCreateUserPassword")
            .RequireAuthorization()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapChangeUserPassword(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPatch("/",
            async (IUserPasswordAppService appService, [FromBody] UserPasswordViewModel viewModel,
            CancellationToken ct) =>
            {
                var result = await appService.ChangePasswordAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("ChangeUserPassword")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapUpdateUserPassword(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPut("/",
            async (IUserPasswordAppService appService, [FromBody] UserPasswordViewModel viewModel,
            CancellationToken ct) =>
            {
                var result = await appService.UpdateAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("UpdateUserPassword")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapDeleteUserPassword(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapDelete("/",
            async (IUserPasswordAppService appService, [FromBody] UserPasswordViewModel viewModel,
            CancellationToken ct) =>
            {
                var result = await appService.RemoveAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.Problem(result.Error);
            })
            .WithName("DeleteUserPassword")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
