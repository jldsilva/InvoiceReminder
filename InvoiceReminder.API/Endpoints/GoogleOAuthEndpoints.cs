using InvoiceReminder.ExternalServices.Gmail;

namespace InvoiceReminder.API.Endpoints;

public class GoogleOAuthEndpoints : IEndpointDefinition
{
    private const string basepath = "/api/google_oauth";

    public void RegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        var endpoint = endpoints.MapGroup(basepath).WithName("GoogleOAuthEndpoints");

        MapGetAuthUrl(endpoint);
        MapAuthorize(endpoint);
        MapRevoke(endpoint);
    }

    private static void MapGetAuthUrl(RouteGroupBuilder endpoint)
    {
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
    }

    private static void MapAuthorize(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/authorize",
            async (IGoogleOAuthService oAuthService, CancellationToken ct, Guid state, string code) =>
            {
                var result = await oAuthService.GrantAuthorizationAsync(state, code, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("Authorize")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapRevoke(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapDelete("/revoke", async (IGoogleOAuthService oAuthService, CancellationToken ct, Guid id) =>
            {
                var result = await oAuthService.RevokeAuthorizationAsync(id, ct);

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
