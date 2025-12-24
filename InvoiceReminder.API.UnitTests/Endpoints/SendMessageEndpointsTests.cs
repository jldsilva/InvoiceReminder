using InvoiceReminder.API.UnitTests.Factories;
using InvoiceReminder.ExternalServices.SendMessage;
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
public sealed class SendMessageEndpointsTests
{
    private readonly HttpClient _client;
    private readonly IAuthorizationService _authorizationService;
    private readonly ISendMessageService _sendMessageService;
    private const string basepath = "/api/send_message";

    public TestContext TestContext { get; set; }

    public SendMessageEndpointsTests()
    {
        var factory = new CustomWebApplicationFactory<Program>();
        var serviceProvider = factory.Services;

        _client = factory.CreateClient();
        _authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        _sendMessageService = serviceProvider.GetRequiredService<ISendMessageService>();
    }

    #region MapGet Tests

    [TestMethod]
    public async Task SendMessage_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = "Total messages sent: 1";

        _ = _sendMessageService.SendMessage(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResult));

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Assert
        _ = _sendMessageService.Received(1).SendMessage(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        result.ShouldSatisfyAllConditions(result =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<string>();
            result.ShouldContain("Total messages sent");
        });
    }

    [TestMethod]
    public async Task SendMessage_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{userId}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task SendMessage_WhenUserIsAuthenticatedButServiceReturnsEmptyResult_ShouldReturnInternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _sendMessageService.SendMessage(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(string.Empty));

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _sendMessageService.Received(1).SendMessage(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        result.ShouldSatisfyAllConditions(r =>
        {
            _ = r.ShouldNotBeNull();
            _ = r.ShouldBeOfType<ProblemDetails>();
            r.Title.ShouldBe("Erro ao enviar mensagem");
            r.Detail.ShouldContain("O serviço não retornou um resultado válido");
        });
    }

    [TestMethod]
    public async Task SendMessage_WhenUserIsAuthenticatedButServiceReturnsNull_ShouldReturnInternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _sendMessageService.SendMessage(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string>(null));

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _sendMessageService.Received(1).SendMessage(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        result.ShouldSatisfyAllConditions(r =>
        {
            _ = r.ShouldNotBeNull();
            _ = r.ShouldBeOfType<ProblemDetails>();
        });
    }

    [TestMethod]
    public async Task SendMessage_WhenUserIsAuthenticatedButServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _sendMessageService.SendMessage(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Service error"));

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _sendMessageService.Received(1).SendMessage(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        result.ShouldSatisfyAllConditions(r =>
        {
            _ = r.ShouldNotBeNull();
            _ = r.ShouldBeOfType<ProblemDetails>();
        });
    }

    [TestMethod]
    public async Task SendMessage_WithValidUserId_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = "Total messages sent: 3";

        _ = _sendMessageService.SendMessage(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResult));

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        _ = _sendMessageService.Received(1).SendMessage(
            Arg.Is<Guid>(id => id == userId),
            Arg.Any<CancellationToken>()
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task SendMessage_WhenServiceReturnsNoMessages_ShouldStillReturnOkWithValidResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = "Total messages sent: 0";

        _ = _sendMessageService.SendMessage(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResult));

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Assert
        _ = _sendMessageService.Received(1).SendMessage(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        result.Trim('"').ShouldSatisfyAllConditions(r =>
        {
            _ = r.ShouldNotBeNull();
            r.ShouldBe(expectedResult);
        });
    }

    [TestMethod]
    public async Task SendMessage_WhenServiceReturnsWarningMessage_ShouldReturnOkWithMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = $"No Authentication Token found for userId: {userId}";

        _ = _sendMessageService.SendMessage(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResult));

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Assert
        _ = _sendMessageService.Received(1).SendMessage(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        result.ShouldSatisfyAllConditions(r =>
        {
            _ = r.ShouldNotBeNull();
            r.ShouldContain("No Authentication Token found");
        });
    }

    #endregion
}
