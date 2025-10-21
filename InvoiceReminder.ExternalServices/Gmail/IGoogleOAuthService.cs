using Google.Apis.Auth.OAuth2;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.ExternalServices.Gmail;

public interface IGoogleOAuthService
{
    Task<UserCredential> AuthenticateAsync(EmailAuthToken authToken, CancellationToken cancellationToken = default);
    Result<string> GetAuthorizationUrl(string state);
    Task<Result<UserCredential>> GrantAuthorizationAsync(Guid userId, string authCode, CancellationToken cancellationToken = default);
    Task<Result<string>> RevokeAuthorizationAsync(Guid userId, CancellationToken cancellationToken = default);
}
