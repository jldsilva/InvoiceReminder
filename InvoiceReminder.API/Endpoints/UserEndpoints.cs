using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceReminder.API.Endpoints;

public class UserEndpoints : IEndpointDefinition
{
    private const string basepath = "/api/user";

    public void RegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        var endpoint = endpoints.MapGroup(basepath).WithName("UserEndpoints");

        MapGetUsers(endpoint);
        MapGetUser(endpoint);
        MapGetUserByEmail(endpoint);
        MapCreateUser(endpoint);
        MapCreateUsers(endpoint);
        MapUpdateUser(endpoint);
        MapUpdateBasicUserInfo(endpoint);
        MapDeleteUser(endpoint);
    }

    private static void MapGetUsers(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/", (IUserAppService appService) =>
            {
                var result = appService.GetAll();

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("GetUsers")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapGetUser(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/{id}", async (IUserAppService appService, Guid id, CancellationToken ct) =>
            {
                var result = await appService.GetByIdAsync(id, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetUser")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapGetUserByEmail(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/getby-email/{value}",
            async (IUserAppService appService, string value, CancellationToken ct) =>
            {
                var result = await appService.GetByEmailAsync(value, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetUserByEmail")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapCreateUser(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPost("/",
            async (IUserAppService appService, [FromBody] UserViewModel viewModel, CancellationToken ct) =>
            {
                var result = await appService.AddAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.Created($"{basepath}/{result.Value.Id}", result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("CreateUser")
            .AllowAnonymous()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapCreateUsers(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPost("/bulk-insert",
            async (IUserAppService appService, [FromBody] ICollection<UserViewModel> viewModelCollection,
            CancellationToken ct) =>
            {
                var result = await appService.BulkInsertAsync(viewModelCollection, ct);

                return result.IsSuccess
                    ? Results.Created($"{basepath}", result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("CreateUsers")
            .RequireAuthorization()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapUpdateUser(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPut("/",
            async (IUserAppService appService, [FromBody] UserViewModel viewModel, CancellationToken ct) =>
            {
                var result = await appService.UpdateAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("UpdateUser")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapUpdateBasicUserInfo(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPatch("/",
            async (IUserAppService appService, [FromBody] UserViewModel viewModel, CancellationToken ct) =>
            {
                var result = await appService.UpdateBasicUserInfoAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("UpdateBasicUserInfo")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapDeleteUser(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapDelete("/",
            async (IUserAppService appService, [FromBody] UserViewModel viewModel, CancellationToken ct) =>
            {
                var result = await appService.RemoveAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.Problem(result.Error);
            })
            .WithName("DeleteUser")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
