using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Authentication.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceReminder.API.Endpoints;

public class UserEndpoints : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        var endpoint = endpoints.MapGroup("/api/user").WithName("UserEndpoints");

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

        _ = endpoint.MapGet("/{id}", async (IUserAppService userAppService, Guid id) =>
        {
            var result = await userAppService.GetByIdAsync(id);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(result.Error);
        })
            .WithName("GetUser")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapGet("/get_by_email/{value}", async (IUserAppService userAppService, string value) =>
            {
                var result = await userAppService.GetByEmailAsync(value);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetUserByEmail")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapPost("/", async (IUserAppService userAppService, [FromBody] UserViewModel userViewModel) =>
            {
                userViewModel.Password = userViewModel.Password.ToSHA256();

                var result = await userAppService.AddAsync(userViewModel);

                return result.IsSuccess
                    ? Results.Created($"/api/user/{result.Value.Email}", result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("CreateUser")
            .RequireAuthorization()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapPost("/bulk_insert", async (IUserAppService userAppService,
            [FromBody] ICollection<UserViewModel> usersViewModel) =>
            {
                foreach (var user in usersViewModel)
                {
                    user.Password = user.Password.ToSHA256();
                }

                var result = await userAppService.BulkInsertAsync(usersViewModel);

                return result.IsSuccess
                    ? Results.Created("/api/user/", result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("CreateUsers")
            .RequireAuthorization()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapPut("/", async (IUserAppService userAppService, [FromBody] UserViewModel userViewModel) =>
            {
                var result = await userAppService.UpdateAsync(userViewModel);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("UpdateUser")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapDelete("/", async (IUserAppService userAppService, [FromBody] UserViewModel userViewModel) =>
            {
                var result = await userAppService.RemoveAsync(userViewModel);

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
