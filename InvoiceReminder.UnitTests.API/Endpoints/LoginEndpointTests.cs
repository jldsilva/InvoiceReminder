using Bogus;
using InvoiceReminder.API.AuthenticationSetup;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Authentication.Extensions;
using InvoiceReminder.Authentication.Interfaces;
using InvoiceReminder.Authentication.Jwt;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.UnitTests.API.Factories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace InvoiceReminder.UnitTests.API.Endpoints;

[TestClass]
public sealed class LoginEndpointTests
{
    private readonly HttpClient _client;
    private readonly IJwtProvider _jwtProvider;
    private readonly IUserAppService _userAppService;
    private readonly Faker<UserViewModel> _userViewModelFaker;
    private readonly Faker<LoginRequest> _loginRequestFaker;
    private readonly Faker _faker;
    private const string basepath = "/api/login";

    public TestContext TestContext { get; set; }

    public LoginEndpointTests()
    {
        var factory = new CustomWebApplicationFactory<Program>();
        var serviceProvider = factory.Services;

        _client = factory.CreateClient();
        _jwtProvider = serviceProvider.GetRequiredService<IJwtProvider>();
        _userAppService = serviceProvider.GetRequiredService<IUserAppService>();
        _faker = new Faker();

        _userViewModelFaker = new Faker<UserViewModel>()
            .RuleFor(u => u.Id, faker => faker.Random.Guid())
            .RuleFor(u => u.Name, faker => faker.Person.FullName)
            .RuleFor(u => u.Email, faker => faker.Internet.Email())
            .RuleFor(u => u.Password, faker => faker.Internet.Password().ToSHA256())
            .RuleFor(u => u.TelegramChatId, faker => faker.Random.Long(1))
            .RuleFor(u => u.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(u => u.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());

        _loginRequestFaker = new Faker<LoginRequest>()
            .RuleFor(l => l.Email, faker => faker.Internet.Email())
            .RuleFor(l => l.Password, faker => faker.Internet.Password());
    }

    [TestMethod]
    public async Task Login_WithValidCredentials_ReturnsOkWithJwtObject()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, basepath);
        var password = _faker.Internet.Password();

        var expectedUser = _userViewModelFaker
            .Clone()
            .RuleFor(u => u.Password, password.ToSHA256())
            .Generate();

        var loginRequest = new LoginRequest
        {
            Email = expectedUser.Email,
            Password = password
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

        var loginRequest = _loginRequestFaker.Generate();

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

        var expectedUser = _userViewModelFaker.Generate();

        var loginRequest = new LoginRequest
        {
            Email = expectedUser.Email,
            Password = _faker.Internet.Password()
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

        var loginRequest = _loginRequestFaker.Generate();

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
