using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Gmail.v1;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.ExternalServices.Gmail;
using InvoiceReminder.UnitTests.API.Factories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace InvoiceReminder.UnitTests.API.Endpoints;

[TestClass]
public sealed class GoogleOAuthEndpointsTests
{
    private readonly HttpClient _client;
    private readonly IAuthorizationService _authorizationService;
    private readonly IGoogleOAuthService _oAuthService;
    private const string basepath = "/api/google_oauth";

    public TestContext TestContext { get; set; }

    public GoogleOAuthEndpointsTests()
    {
        var factory = new CustomWebApplicationFactory<Program>();
        var serviceProvider = factory.Services;

        _client = factory.CreateClient();
        _authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        _oAuthService = serviceProvider.GetRequiredService<IGoogleOAuthService>();
    }

    #region GetAuthUrl Tests

    [TestMethod]
    public async Task GetAuthUrl_WhenUserIsAuthenticated_ShouldReturnOkWithUrl()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/get-auth-url/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        _ = _oAuthService.GetAuthorizationUrl(Arg.Any<string>())
            .Returns(Result<string>.Success("https://accounts.google.com/o/oauth2/v2/auth"));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        var url = result.Trim('"');

        // Assert
        _ = _oAuthService.Received(1).GetAuthorizationUrl(Arg.Any<string>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        url.ShouldNotBeNullOrEmpty();
        url.ShouldStartWith("https://accounts.google.com/o/oauth2/v2/auth");
    }

    [TestMethod]
    public async Task GetAuthUrl_WhenUserIsAuthenticated_ShouldPassUserIdToService()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/get-auth-url/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        _ = _oAuthService.GetAuthorizationUrl(Arg.Any<string>())
            .Returns(Result<string>.Success("https://accounts.google.com/o/oauth2/v2/auth"));

        // Act
        _ = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        _ = _oAuthService.Received(1).GetAuthorizationUrl(Arg.Is<string>(s => s == id.ToString()));
    }

    [TestMethod]
    public async Task GetAuthUrl_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/get-auth-url/{id}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetAuthUrl_WhenUserIsAuthenticatedButServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/get-auth-url/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        _ = _oAuthService.GetAuthorizationUrl(Arg.Any<string>()).Throws<ArgumentException>();

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _oAuthService.Received(1).GetAuthorizationUrl(Arg.Any<string>());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetAuthUrl_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/get-auth-url/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        _ = _oAuthService.GetAuthorizationUrl(Arg.Any<string>()).Throws<ApplicationException>();

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _oAuthService.Received(1).GetAuthorizationUrl(Arg.Any<string>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetAuthUrl_WhenServiceReturnsFailure_ShouldReturnProblem()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/get-auth-url/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var errorResult = Result<string>.Failure("Authorization URL generation failed");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        _ = _oAuthService.GetAuthorizationUrl(Arg.Any<string>())
            .Returns(errorResult);

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    #endregion

    #region Authorize Tests

    [TestMethod]
    public async Task Authorize_ReturnsOkWithUserCredential()
    {
        // Arrange
        var state = Guid.NewGuid();
        var code = "test_code";
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = "random_id",
                ClientSecret = "random_secret"
            },
            Scopes = [GmailService.Scope.GmailReadonly]
        });
        var userCredential = new UserCredential(flow, "", new Google.Apis.Auth.OAuth2.Responses.TokenResponse());
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/authorize?state={state}&code={code}");

        _ = _oAuthService.GrantAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<UserCredential>.Success(userCredential));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Assert
        _ = _oAuthService.Received(1)
            .GrantAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNullOrWhiteSpace();
        result.ShouldContain("accessToken");
    }

    [TestMethod]
    public async Task Authorize_ShouldPassStateAndCodeToService()
    {
        // Arrange
        var state = Guid.NewGuid();
        var code = "test_code";
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = "random_id",
                ClientSecret = "random_secret"
            },
            Scopes = [GmailService.Scope.GmailReadonly]
        });
        var userCredential = new UserCredential(flow, "", new Google.Apis.Auth.OAuth2.Responses.TokenResponse());
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/authorize?state={state}&code={code}");

        _ = _oAuthService.GrantAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<UserCredential>.Success(userCredential));

        // Act
        _ = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        _ = _oAuthService.Received(1).GrantAuthorizationAsync(
            Arg.Is<Guid>(s => s == state),
            Arg.Is<string>(c => c == code),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Authorize_AllowsAnonymousUsers()
    {
        // Arrange
        var state = Guid.NewGuid();
        var code = "test_code";
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = "random_id",
                ClientSecret = "random_secret"
            },
            Scopes = [GmailService.Scope.GmailReadonly]
        });
        var userCredential = new UserCredential(flow, "", new Google.Apis.Auth.OAuth2.Responses.TokenResponse());
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/authorize?state={state}&code={code}");
        // Note: No authorization header set

        _ = _oAuthService.GrantAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<UserCredential>.Success(userCredential));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Authorize_WhenServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var state = Guid.NewGuid();
        var code = "test_code";
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/authorize?state={state}&code={code}");

        _ = _oAuthService.GrantAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ArgumentException>();

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _oAuthService.Received(1)
            .GrantAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task Authorize_WhenServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var state = Guid.NewGuid();
        var code = "test_code";
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/authorize?state={state}&code={code}");

        _ = _oAuthService.GrantAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ApplicationException>();

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _oAuthService.Received(1)
            .GrantAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task Authorize_WhenServiceReturnsFailure_ShouldReturnProblem()
    {
        // Arrange
        var state = Guid.NewGuid();
        var code = "test_code";
        var errorResult = Result<UserCredential>.Failure("Authorization failed");
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/authorize?state={state}&code={code}");

        _ = _oAuthService.GrantAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(errorResult));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    #endregion

    #region Revoke Tests

    [TestMethod]
    public async Task Revoke_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expectedMessage = "Authorization revoked successfully";
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{basepath}/revoke?id={id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        _ = _oAuthService.RevokeAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success(expectedMessage));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        var message = result.Trim('"');

        // Assert
        _ = _oAuthService.Received(1).RevokeAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        message.ShouldNotBeNullOrWhiteSpace();
        message.ShouldBe(expectedMessage);
    }

    [TestMethod]
    public async Task Revoke_WhenUserIsAuthenticated_ShouldPassIdToService()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expectedMessage = "Authorization revoked successfully";
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{basepath}/revoke?id={id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        _ = _oAuthService.RevokeAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success(expectedMessage));

        // Act
        _ = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        _ = _oAuthService.Received(1).RevokeAuthorizationAsync(
            Arg.Is<Guid>(i => i == id),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Revoke_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{basepath}/revoke?id={id}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task Revoke_WhenUserIsAuthenticatedButServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{basepath}/revoke?id={id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        _ = _oAuthService.RevokeAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ArgumentException>();

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _oAuthService.Received(1).RevokeAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task Revoke_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{basepath}/revoke?id={id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        _ = _oAuthService.RevokeAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ApplicationException>();

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _oAuthService.Received(1).RevokeAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task Revoke_WhenServiceReturnsFailure_ShouldReturnProblem()
    {
        // Arrange
        var id = Guid.NewGuid();
        var errorResult = Result<string>.Failure("Revocation failed");
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{basepath}/revoke?id={id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        _ = _oAuthService.RevokeAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(errorResult));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    #endregion
}
