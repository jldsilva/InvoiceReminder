using InvoiceReminder.API.AuthenticationSetup;
using InvoiceReminder.API.UnitTests.Factories;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Authentication.Extensions;
using InvoiceReminder.Authentication.Interfaces;
using InvoiceReminder.Authentication.Jwt;
using InvoiceReminder.Domain.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace InvoiceReminder.API.UnitTests.Endpoints;

[TestClass]
public class LoginEndpointTests
{
    private readonly HttpClient _client;
    private readonly IJwtProvider _jwtProvider;
    private readonly IUserAppService _userAppService;
    private const string basepath = "/api/login";

    public TestContext TestContext { get; set; }

    public LoginEndpointTests()
    {
        var factory = new CustomWebApplicationFactory<Program>();
        var serviceProvider = factory.Services;

        _client = factory.CreateClient();
        _jwtProvider = serviceProvider.GetRequiredService<IJwtProvider>();
        _userAppService = serviceProvider.GetRequiredService<IUserAppService>();
    }

    [TestMethod]
    public async Task Login_WithValidCredentials_ReturnsOkWithJwtObject()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, basepath);

        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password"
        };

        var expectedUser = new UserViewModel
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password".ToSHA256()
        };

        var expectedJwtObject = new JwtObject
        {
            Authenticated = true,
            AuthenticationToken = "jwt_test_token",
            Expiration = DateTime.UtcNow.AddMinutes(60)
        };

        _ = _userAppService.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<UserViewModel>.Success(expectedUser));

        _ = _jwtProvider.Generate(Arg.Any<UserViewModel>()).Returns(expectedJwtObject);

        // Act
        request.Content = JsonContent.Create(loginRequest);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<JwtObject>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _ = _jwtProvider.Received(1).Generate(Arg.Any<UserViewModel>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        result.ShouldSatisfyAllConditions(result =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<JwtObject>();
            result.ShouldBeEquivalentTo(expectedJwtObject);
        });
    }

    [TestMethod]
    public async Task Login_WithInvalidEmail_ReturnsUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, basepath);

        var loginRequest = new LoginRequest
        {
            Email = "invalid@example.com",
            Password = "password"
        };

        var expectedJwtObject = new JwtObject
        {
            Authenticated = false,
            AuthenticationToken = null
        };

        _ = _userAppService.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<UserViewModel>.Failure("User not found"));

        // Act
        request.Content = JsonContent.Create(loginRequest);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<JwtObject>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _ = _jwtProvider.DidNotReceive().Generate(Arg.Any<UserViewModel>());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        result.ShouldSatisfyAllConditions(result =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<JwtObject>();
            result.ShouldBeEquivalentTo(expectedJwtObject);
        });
    }

    [TestMethod]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, basepath);

        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrong_password"
        };

        var expectedUser = new UserViewModel
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password".ToSHA256()
        };

        var expectedJwtObject = new JwtObject
        {
            Authenticated = false,
            AuthenticationToken = null
        };

        _ = _userAppService.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<UserViewModel>.Success(expectedUser));

        // Act
        request.Content = JsonContent.Create(loginRequest);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<JwtObject>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _ = _jwtProvider.DidNotReceive().Generate(Arg.Any<UserViewModel>());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        result.ShouldSatisfyAllConditions(result =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<JwtObject>();
            result.ShouldBeEquivalentTo(expectedJwtObject);
        });
    }

    [TestMethod]
    public async Task Login_WithEmptyEmailOrPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, basepath);

        var loginRequest = new LoginRequest
        {
            Email = string.Empty,
            Password = string.Empty
        };

        // Act
        request.Content = JsonContent.Create(loginRequest);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<string>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.DidNotReceive().GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _ = _jwtProvider.DidNotReceive().Generate(Arg.Any<UserViewModel>());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        result.ShouldSatisfyAllConditions(result =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<string>();
            result.ShouldBe("Email e senha são obrigatórios");
        });
    }

    [TestMethod]
    public async Task Login_WhenServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, basepath);

        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password"
        };

        _ = _userAppService.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Service error"));

        // Act
        request.Content = JsonContent.Create(loginRequest);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _ = _jwtProvider.DidNotReceive().Generate(Arg.Any<UserViewModel>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        result.ShouldSatisfyAllConditions(result =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<ProblemDetails>();
        });
    }
}
