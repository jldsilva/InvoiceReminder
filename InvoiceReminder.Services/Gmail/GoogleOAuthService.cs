using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using InvoiceReminder.Domain.Services.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace InvoiceReminder.ExternalServices.Gmail;

[ExcludeFromCodeCoverage]
public class GoogleOAuthService : IGoogleOAuthService
{
    private readonly ILogger<GoogleOAuthService> _logger;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public GoogleOAuthService(ILogger<GoogleOAuthService> logger, IConfigurationService configurationService)
    {
        _logger = logger;
        _clientId = configurationService.GetSecret("appKeys", "googleOauthClientId");
        _clientSecret = configurationService.GetSecret("appKeys", "googleOauthClientSecret");
    }

    public async Task<(string, UserCredential)> AuthorizeAsync(string userEmail)
    {
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets
            {
                ClientId = _clientId,
                ClientSecret = _clientSecret
            },
            ["email", "profile", "https://mail.google.com/"],
            userEmail,
            CancellationToken.None
        );

        if (credential.Token.IsStale)
        {
            _logger.LogInformation("Token is stale, refreshing...");

            credential.Token = await RefreshAccessTokenAsync(credential.Token.RefreshToken);
        }

        var jwtPayload = await GoogleJsonWebSignature.ValidateAsync(credential.Token.IdToken);

        return (jwtPayload.Email, credential);
    }

    public async Task<TokenResponse> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _clientId,
                ClientSecret = _clientSecret
            }
        });

        return await flow.RefreshTokenAsync(string.Empty, refreshToken, cancellationToken);
    }

    public async Task RevokeAcessToken(string userEmail, string token, CancellationToken cancellationToken = default)
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _clientId,
                ClientSecret = _clientSecret
            }
        });

        await flow.RevokeTokenAsync(userEmail, token, cancellationToken);
    }
}
