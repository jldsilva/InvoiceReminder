using InvoiceReminder.API.AuthenticationSetup;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Authentication.Extensions;
using InvoiceReminder.Authentication.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceReminder.API.Endpoints;

public class LoginEndpoints : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapPost("/api/login",
            async (IJwtProvider jwtProvider,
                IUserAppService userAppService,
                CancellationToken ct,
                [FromBody] LoginRequest request) =>
            {
                if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request?.Password))
                {
                    return Results.BadRequest("Email e senha são obrigatórios");
                }

                var result = await userAppService.GetByEmailAsync(request.Email, ct);

                var isValid = result.IsSuccess
                    && request.Password.ToSHA256().Equals(result.Value.Password);

                return isValid
                    ? Results.Ok(jwtProvider.Generate(result.Value))
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
