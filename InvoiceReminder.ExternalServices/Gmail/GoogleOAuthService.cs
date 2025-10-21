using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Services.Configuration;
using InvoiceReminder.Domain.Services.TokenCrypto;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace InvoiceReminder.ExternalServices.Gmail;

[ExcludeFromCodeCoverage]
public class GoogleOAuthService : IGoogleOAuthService
{
    private readonly ILogger<GoogleOAuthService> _logger;
    private readonly IEmailAuthTokenRepository _tokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly GoogleAuthorizationCodeFlow _flow;
    private readonly string _redirectUri;
    private readonly byte[] _key;

    public GoogleOAuthService(
        IEmailAuthTokenRepository tokenRepository,
        IConfigurationService configurationService,
        IUnitOfWork unitOfWork,
        ILogger<GoogleOAuthService> logger)
    {
        _flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = configurationService.GetSecret("appKeys", "googleOauthClientId"),
                ClientSecret = configurationService.GetSecret("appKeys", "googleOauthClientSecret")
            },
            Scopes = [GmailService.Scope.GmailModify]
        });

        _key = Convert.FromBase64String(configurationService.GetSecret("appKeys", "tokenEncryptionKey"));
        _logger = logger;
        _tokenRepository = tokenRepository;
        _unitOfWork = unitOfWork;
        _redirectUri = configurationService.GetSecret("appKeys", "googleOauthRedirectUri");
    }

    public async Task<UserCredential> AuthenticateAsync(
        EmailAuthToken authToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authToken);

        if (!authToken.IsStale)
        {
            return new UserCredential(_flow, authToken.UserId.ToString(), new TokenResponse
            {
                AccessToken = authToken.AccessToken,
                RefreshToken = TokenCryptoService.Decrypt(authToken.RefreshToken, authToken.NonceValue, _key),
                ExpiresInSeconds = (long?)(authToken.AccessTokenExpiry - DateTime.UtcNow).TotalSeconds
            });
        }

        var tokenResponse = await RefreshAuthTokenAsync(authToken, cancellationToken);

        return new UserCredential(_flow, authToken.UserId.ToString(), tokenResponse);
    }

    public Result<string> GetAuthorizationUrl(string state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);

        var authorizationCodeRequest = _flow.CreateAuthorizationCodeRequest(_redirectUri);
        authorizationCodeRequest.Scope = GmailService.Scope.GmailModify;
        authorizationCodeRequest.State = state;

        var authorizationUrl = authorizationCodeRequest.Build();

        return string.IsNullOrWhiteSpace(authorizationUrl.AbsoluteUri)
            ? Result<string>.Failure("Failed to generate authorization URL")
            : Result<string>.Success(authorizationUrl.AbsoluteUri);
    }

    public async Task<Result<UserCredential>> GrantAuthorizationAsync(
        Guid userId, string authCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenResponse = await _flow.ExchangeCodeForTokenAsync(
            userId.ToString(),
            authCode,
            _redirectUri,
            cancellationToken);

            var (encryptedToken, nonceValue) = TokenCryptoService.Encrypt(tokenResponse.RefreshToken, _key);

            var emailAuthToken = new EmailAuthToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = encryptedToken,
                NonceValue = nonceValue,
                TokenProvider = "Google",
                AccessTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds ?? 3600)
            };

            _ = await _tokenRepository.AddAsync(emailAuthToken, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<UserCredential>.Success(new UserCredential(_flow, userId.ToString(), tokenResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting authorization for user {UserId}", userId);

            return Result<UserCredential>.Failure("Error granting authorization for user");
        }
    }

    public async Task<Result<string>> RevokeAuthorizationAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailAuthToken = await _tokenRepository.GetByUserIdAsync(userId, "Google");

            if (emailAuthToken == null)
            {
                return Result<string>.Failure($"No Authorization Token to revoke to the given Id...");
            }

            await _flow.RevokeTokenAsync(userId.ToString(), emailAuthToken.AccessToken, cancellationToken);

            _tokenRepository.Remove(emailAuthToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<string>.Success("Authorization Token revoked successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Revoking Authorization Token failed: {Message}", ex.Message);

            return Result<string>.Failure("Error revoking Authorization Token.");
        }
    }

    private async Task<TokenResponse> RefreshAuthTokenAsync(
        EmailAuthToken authToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentRefreshToken = TokenCryptoService.Decrypt(authToken.RefreshToken, authToken.NonceValue, _key);
            var tokenResponse = await _flow.RefreshTokenAsync(authToken.UserId.ToString(), currentRefreshToken, cancellationToken);
            var (encryptedRefreshToken, nonceValue) = TokenCryptoService.Encrypt(tokenResponse.RefreshToken, _key);

            authToken.AccessToken = tokenResponse.AccessToken;
            authToken.RefreshToken = encryptedRefreshToken;
            authToken.NonceValue = nonceValue;
            authToken.AccessTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds ?? 3600);

            _ = _tokenRepository.Update(authToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return tokenResponse;
        }
        catch (Exception)
        {
            _tokenRepository.Remove(authToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw;
        }
    }
}
