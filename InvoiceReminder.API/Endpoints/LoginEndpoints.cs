using InvoiceReminder.API.AuthenticationSetup;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Authentication.Abstractions;
using InvoiceReminder.Authentication.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceReminder.API.Endpoints;

public class LoginEndpoints : IEndpointDefinition
{
    private const string basepath = "/api/login";

    public void RegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        var endpoint = endpoints.MapGroup(basepath).WithName("LoginEndpoints");

        MapLogin(endpoint);
    }

    private static void MapLogin(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapPost("/",
            async (IUserAppService appService, IJwtProvider jwtProvider, [FromBody] LoginRequest request,
            CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request?.Password))
                {
                    return Results.BadRequest("Email e senha são obrigatórios");
                }

                var result = await appService.ValidateUserPasswordAsync(request.Email, request.Password, ct);

                return result.IsSuccess
                    ? Results.Ok(jwtProvider.Generate(new UserClaims
                    {
                        Id = result.Value.Id,
                        Name = result.Value.Name,
                        Email = result.Value.Email,
                        TelegramChatId = result.Value.TelegramChatId
                    }))
                    : Results.Unauthorized();
            })
            .WithName("Login")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
