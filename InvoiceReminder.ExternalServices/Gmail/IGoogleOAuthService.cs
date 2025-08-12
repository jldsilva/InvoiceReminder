using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;

namespace InvoiceReminder.ExternalServices.Gmail;

public interface IGoogleOAuthService
{
    Task<(string, UserCredential)> AuthorizeAsync(string userEmail);
    Task<TokenResponse> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAcessToken(string userEmail, string token, CancellationToken cancellationToken = default);
}
