using InvoiceReminder.ExternalServices.Gmail;

namespace InvoiceReminder.API.Endpoints;

public class GoogleOAuthEndpoints : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        var endpoint = endpoints.MapGroup("/api/google_oauth").WithName("GoogleOAuthEndpoints");

        _ = endpoint.MapGet("/get-auth-url/{id}", (IGoogleOAuthService oAuthService, Guid id) =>
            {
                var result = oAuthService.GetAuthorizationUrl(id.ToString());

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("GetAuthUrl")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapGet("/authorize", async (IGoogleOAuthService oAuthService, Guid state, string code) =>
            {
                var result = await oAuthService.GrantAuthorizationAsync(state, code);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("Authorize")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapDelete("/revoke", async (IGoogleOAuthService oAuthService, Guid id) =>
            {
                var result = await oAuthService.RevokeAuthorizationAsync(id);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("Revoke")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
