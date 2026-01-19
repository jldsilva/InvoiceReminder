using Bogus;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.UnitTests.API.Factories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace InvoiceReminder.UnitTests.API.Endpoints;

[TestClass]
public sealed class UserPasswordEndpointsTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly IAuthorizationService _authorizationService;
    private readonly IUserPasswordAppService _userPasswordAppService;
    private readonly Faker<UserPasswordViewModel> _userPasswordViewModelFaker;
    private const string basepath = "/api/user_password";

    public TestContext TestContext { get; set; }

    public UserPasswordEndpointsTests()
    {
        _factory = new CustomWebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        _authorizationService = _factory.Services.GetRequiredService<IAuthorizationService>();
        _userPasswordAppService = _factory.Services.GetRequiredService<IUserPasswordAppService>();

        _userPasswordViewModelFaker = new Faker<UserPasswordViewModel>()
            .RuleFor(u => u.Id, faker => faker.Random.Guid())
            .RuleFor(u => u.UserId, faker => faker.Random.Guid())
            .RuleFor(u => u.PasswordHash, faker => faker.Internet.Password(12, false, "[A-Z]", "abc123"))
            .RuleFor(u => u.PasswordSalt, faker => faker.Random.AlphaNumeric(24))
            .RuleFor(u => u.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(u => u.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
    }

    #region MapPost Tests - CreateUserPassword

    [TestMethod]
    public async Task CreateUserPassword_WhenUserIsAuthenticated_ShouldReturnCreated()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var userPasswordViewModel = _userPasswordViewModelFaker.Generate();
        var expectedResult = Result<UserPasswordViewModel>.Success(userPasswordViewModel);

        _ = _userPasswordAppService.AddAsync(Arg.Any<UserPasswordViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(userPasswordViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<UserPasswordViewModel>(TestContext.CancellationToken);

        // Assert
        _ = _userPasswordAppService.Received(1).AddAsync(Arg.Any<UserPasswordViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<UserPasswordViewModel>();
    }

    [TestMethod]
    public async Task CreateUserPassword_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, basepath);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task CreateUserPassword_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var userPasswordViewModel = _userPasswordViewModelFaker.Generate();
        var expectedResult = Result<UserPasswordViewModel>.Failure("Service error");

        _ = _userPasswordAppService.AddAsync(Arg.Any<UserPasswordViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(userPasswordViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _userPasswordAppService.Received(1).AddAsync(Arg.Any<UserPasswordViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    #endregion

    #region MapPost Tests - BulkCreateUserPassword

    [TestMethod]
    public async Task BulkCreateUserPassword_WhenUserIsAuthenticated_ShouldReturnCreated()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, $"{basepath}/bulk-insert");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var userPasswordViewModels = _userPasswordViewModelFaker.Generate(2);
        var expectedResult = Result<int>.Success(userPasswordViewModels.Count);

        _ = _userPasswordAppService.BulkInsertAsync(Arg.Any<ICollection<UserPasswordViewModel>>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(userPasswordViewModels);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = int.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));

        // Assert
        _ = _userPasswordAppService.Received(1).BulkInsertAsync(Arg.Any<ICollection<UserPasswordViewModel>>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        result.ShouldBeGreaterThan(0);
        _ = result.ShouldBeOfType<int>();
    }

    [TestMethod]
    public async Task BulkCreateUserPassword_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, $"{basepath}/bulk-insert");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task BulkCreateUserPassword_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, $"{basepath}/bulk-insert");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var userPasswordViewModels = _userPasswordViewModelFaker.Generate(2);
        var expectedResult = Result<int>.Failure("Service error");

        _ = _userPasswordAppService.BulkInsertAsync(Arg.Any<ICollection<UserPasswordViewModel>>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(userPasswordViewModels);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _userPasswordAppService.Received(1).BulkInsertAsync(Arg.Any<ICollection<UserPasswordViewModel>>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    #endregion

    #region MapPut Tests - UpdateUserPassword

    [TestMethod]
    public async Task UpdateUserPassword_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var userPasswordViewModel = _userPasswordViewModelFaker.Generate();
        var expectedResult = Result<UserPasswordViewModel>.Success(userPasswordViewModel);

        _ = _userPasswordAppService.UpdateAsync(Arg.Any<UserPasswordViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(userPasswordViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<UserPasswordViewModel>(TestContext.CancellationToken);

        // Assert
        _ = _userPasswordAppService.Received(1).UpdateAsync(Arg.Any<UserPasswordViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<UserPasswordViewModel>();
    }

    [TestMethod]
    public async Task UpdateUserPassword_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, basepath);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task UpdateUserPassword_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var userPasswordViewModel = _userPasswordViewModelFaker.Generate();
        var expectedResult = Result<UserPasswordViewModel>.Failure("Service error");

        _ = _userPasswordAppService.UpdateAsync(Arg.Any<UserPasswordViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(userPasswordViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _userPasswordAppService.Received(1).UpdateAsync(Arg.Any<UserPasswordViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    #endregion

    #region MapDelete Tests - DeleteUserPassword

    [TestMethod]
    public async Task DeleteUserPassword_WhenUserIsAuthenticated_ShouldReturnNoContent()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<UserPasswordViewModel>.Success(null);

        _ = _userPasswordAppService.RemoveAsync(Arg.Any<UserPasswordViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var userPasswordViewModel = _userPasswordViewModelFaker.Generate();
        request.Content = JsonContent.Create(userPasswordViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Assert
        _ = _userPasswordAppService.Received(1).RemoveAsync(Arg.Any<UserPasswordViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task DeleteUserPassword_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, basepath);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task DeleteUserPassword_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<UserPasswordViewModel>.Failure("Service error");

        _ = _userPasswordAppService.RemoveAsync(Arg.Any<UserPasswordViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var userPasswordViewModel = _userPasswordViewModelFaker.Generate();
        request.Content = JsonContent.Create(userPasswordViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _userPasswordAppService.Received(1).RemoveAsync(Arg.Any<UserPasswordViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    #endregion

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
