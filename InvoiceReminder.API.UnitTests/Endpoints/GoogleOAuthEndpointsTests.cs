using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Gmail.v1;
using InvoiceReminder.API.UnitTests.Factories;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.ExternalServices.Gmail;
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

namespace InvoiceReminder.API.UnitTests.Endpoints;

[TestClass]
public sealed class GoogleOAuthEndpointsTests
{
    private readonly HttpClient _client;
    private readonly IAuthorizationService _authorizationService;
    private readonly IGoogleOAuthService _oAuthService;

    public TestContext TestContext { get; set; }

    public GoogleOAuthEndpointsTests()
    {
        var factory = new CustomWebApplicationFactory<Program>();
        var serviceProvider = factory.Services;

        _client = factory.CreateClient();
        _authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        _oAuthService = serviceProvider.GetRequiredService<IGoogleOAuthService>();
    }

    [TestMethod]
    public async Task GetAuthUrl_WhenUserIsAuthenticated_ShouldReturnOkWithUrl()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/google_oauth/get-auth-url/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        _ = _oAuthService.GetAuthorizationUrl(Arg.Any<string>())
            .Returns(Result<string>.Success("https://accounts.google.com/o/oauth2/v2/auth"));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        var url = result.Trim('"');

        // Assert
        _ = _oAuthService.Received(1).GetAuthorizationUrl(Arg.Any<string>());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        url.ShouldNotBeNullOrEmpty();
        url.ShouldStartWith("https://accounts.google.com/o/oauth2/v2/auth");
    }

    [TestMethod]
    public async Task GetAuthUrl_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/google_oauth/get-auth-url/{id}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetAuthUrl_WhenUserIsAuthenticatedButServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/google_oauth/get-auth-url/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        _ = _oAuthService.GetAuthorizationUrl(Arg.Any<string>()).Throws<ArgumentException>();

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationTokenSource.Token);

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
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/google_oauth/get-auth-url/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        _ = _oAuthService.GetAuthorizationUrl(Arg.Any<string>()).Throws<ApplicationException>();

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _oAuthService.Received(1).GetAuthorizationUrl(Arg.Any<string>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

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
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/google_oauth/authorize?state={state}&code={code}");

        _ = _oAuthService.GrantAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(Result<UserCredential>.Success(userCredential));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _oAuthService.Received(1).GrantAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<string>());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNullOrWhiteSpace();
        result.ShouldContain("accessToken");
    }

    [TestMethod]
    public async Task Authorize_WhenServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var state = Guid.NewGuid();
        var code = "test_code";
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/google_oauth/authorize?state={state}&code={code}");

        _ = _oAuthService.GrantAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<string>()).ThrowsAsync<ArgumentException>();

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _oAuthService.Received(1).GrantAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<string>());
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
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/google_oauth/authorize?state={state}&code={code}");

        _ = _oAuthService.GrantAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<string>()).ThrowsAsync<ApplicationException>();

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _oAuthService.Received(1).GrantAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<string>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task Revoke_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expectedMessage = "Authorization revoked successfully";
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/google_oauth/revoke?id={id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        _ = _oAuthService.RevokeAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success(expectedMessage));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        var message = result.Trim('"');

        // Assert
        _ = _oAuthService.Received(1).RevokeAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        message.ShouldNotBeNullOrWhiteSpace();
        message.ShouldBeEquivalentTo(expectedMessage);
    }

    [TestMethod]
    public async Task Revoke_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/google_oauth/revoke?id={id}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task Revoke_WhenUserIsAuthenticatedButServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/google_oauth/revoke?id={id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        _ = _oAuthService.RevokeAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ArgumentException>();

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationTokenSource.Token);

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
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/google_oauth/revoke?id={id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        _ = _oAuthService.RevokeAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ApplicationException>();

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _oAuthService.Received(1).RevokeAuthorizationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }
}
