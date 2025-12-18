using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Authentication.Extensions;
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
        MapDeleteUser(endpoint);
    }

    private static void MapGetUsers(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/", (IUserAppService userAppService) =>
            {
                var result = userAppService.GetAll();

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("GetUsers")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapGetUser(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/{id}", async (IUserAppService userAppService, CancellationToken ct, Guid id) =>
            {
                var result = await userAppService.GetByIdAsync(id, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetUser")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapGetUserByEmail(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/getby-email/{value}",
            async (IUserAppService userAppService, CancellationToken ct, string value) =>
            {
                var result = await userAppService.GetByEmailAsync(value, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetUserByEmail")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapCreateUser(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPost("/",
            async (IUserAppService userAppService, CancellationToken ct, [FromBody] UserViewModel userViewModel) =>
            {
                userViewModel.Password = userViewModel.Password.ToSHA256();

                var result = await userAppService.AddAsync(userViewModel, ct);

                return result.IsSuccess
                    ? Results.Created($"{basepath}/{result.Value.Email}", result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("CreateUser")
            .RequireAuthorization()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapCreateUsers(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPost("/bulk-insert",
            async (IUserAppService userAppService, CancellationToken ct, [FromBody] ICollection<UserViewModel> usersViewModel) =>
            {
                foreach (var user in usersViewModel)
                {
                    user.Password = user.Password.ToSHA256();
                }

                var result = await userAppService.BulkInsertAsync(usersViewModel, ct);

                return result.IsSuccess
                    ? Results.Created($"{basepath}/", result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("CreateUsers")
            .RequireAuthorization()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapUpdateUser(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPut("/",
            async (IUserAppService userAppService, CancellationToken ct, [FromBody] UserViewModel userViewModel) =>
            {
                var result = await userAppService.UpdateAsync(userViewModel, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("UpdateUser")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapDeleteUser(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapDelete("/",
            async (IUserAppService userAppService, CancellationToken ct, [FromBody] UserViewModel userViewModel) =>
            {
                var result = await userAppService.RemoveAsync(userViewModel, ct);

                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.Problem(result.Error);
            })
            .WithName("DeleteUser")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
