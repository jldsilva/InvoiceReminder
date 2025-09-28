using InvoiceReminder.API.UnitTests.Factories;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
public sealed class UserEndpointsTests
{
    private readonly HttpClient _client;
    private readonly IAuthorizationService _authorizationService;
    private readonly IUserAppService _userAppService;

    public TestContext TestContext { get; set; }

    public UserEndpointsTests()
    {
        var factory = new CustomWebApplicationFactory<Program>();
        var serviceProvider = factory.Services;

        _client = factory.CreateClient();
        _authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        _userAppService = serviceProvider.GetRequiredService<IUserAppService>();
    }

    #region MapGet Tests
    [TestMethod]
    public async Task GetAllUsers_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<IEnumerable<UserViewModel>>.Success(
        [
            new() {
                Id = Guid.NewGuid(),
                Name = "Test User_A",
                Email = "test_user_a@mail.com",
                Password = "test_password",
                TelegramChatId = 123456789,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.NewGuid(),
                Name = "Test User_B",
                Email = "test_user_b@mail.com",
                Password = "test_password",
                TelegramChatId = 987654321,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        ]);

        _ = _userAppService.GetAll().Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<UserViewModel>>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).GetAll();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<List<UserViewModel>>();
        result.Count().ShouldBe(expectedResult.Value.Count());
    }

    [TestMethod]
    public async Task GetAllUsers_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/user");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetAllUsers_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<IEnumerable<UserViewModel>>.Failure("Service error");

        _ = _userAppService.GetAll().Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).GetAll();
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetUserById_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/user/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<UserViewModel>.Success(
        new()
        {
            Id = id,
            Name = "Test User_A",
            Email = "test_user_a@mail.com",
            Password = "test_password",
            TelegramChatId = 123456789,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _ = _userAppService.GetByIdAsync(Arg.Any<Guid>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<UserViewModel>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).GetByIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<UserViewModel>();
        result.ShouldBeEquivalentTo(expectedResult.Value);
    }

    [TestMethod]
    public async Task GetUserById_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/user/{Guid.NewGuid()}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetUserById_WhenUserIsAuthenticatedButServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/user/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _userAppService.GetByIdAsync(Arg.Any<Guid>()).ThrowsAsync<ArgumentException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).GetByIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetUserById_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/user/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _userAppService.GetByIdAsync(Arg.Any<Guid>()).ThrowsAsync<ApplicationException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).GetByIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetUserById_WhenUserIsAuthenticatedButNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/user/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<UserViewModel>.Failure($"User with id {id} not Found.");

        _ = _userAppService.GetByIdAsync(Arg.Any<Guid>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).GetByIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task GetUserByEmail_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var value = "test_user@mail.com";
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/user/get_by_email/{value}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<UserViewModel>.Success(
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Test User_A",
            Email = "test_user_a@mail.com",
            Password = "test_password",
            TelegramChatId = 123456789,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _ = _userAppService.GetByEmailAsync(Arg.Any<string>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<UserViewModel>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).GetByEmailAsync(Arg.Any<string>());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<UserViewModel>();
        result.ShouldBeEquivalentTo(expectedResult.Value);
    }

    [TestMethod]
    public async Task GetUserByEmail_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var value = "test_user@mail.com";
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/user/get_by_email/{value}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetUserByEmail_WhenUserIsAuthenticatedButServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var value = "test_user@mail_com";
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/user/get_by_email/{value}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _userAppService.GetByEmailAsync(Arg.Any<string>()).ThrowsAsync<ArgumentException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).GetByEmailAsync(Arg.Any<string>());
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetUserByEmail_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var value = "test_user@mail.com";
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/user/get_by_email/{value}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _userAppService.GetByEmailAsync(Arg.Any<string>()).ThrowsAsync<ApplicationException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).GetByEmailAsync(Arg.Any<string>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetUserByEmail_WhenUserIsAuthenticatedButNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var value = "test_user@mail.com";
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/user/get_by_email/{value}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<UserViewModel>.Failure($"User not found.");

        _ = _userAppService.GetByEmailAsync(Arg.Any<string>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).GetByEmailAsync(Arg.Any<string>());
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }
    #endregion

    //##################################################################################################################

    #region MapPost Tests
    [TestMethod]
    public async Task CreateUser_WhenUserIsAuthenticated_ShouldReturnCreated()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var userViewModel = new UserViewModel
        {
            Id = Guid.NewGuid(),
            Name = "Test User_A",
            Email = "test_user_a@mail.com",
            Password = "test_password",
            TelegramChatId = 123456789,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedResult = Result<UserViewModel>.Success(userViewModel);

        _ = _userAppService.AddAsync(Arg.Any<UserViewModel>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(userViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<UserViewModel>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).AddAsync(Arg.Any<UserViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<UserViewModel>();
    }

    [TestMethod]
    public async Task CreateUser_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/user");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task CreateUser_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var userViewModel = new UserViewModel
        {
            Id = Guid.NewGuid(),
            Name = "Test User_A",
            Email = "test_user_a@mail.com",
            Password = "test_password",
            TelegramChatId = 123456789,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedResult = Result<UserViewModel>.Failure("Service error");

        _ = _userAppService.AddAsync(Arg.Any<UserViewModel>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(userViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).AddAsync(Arg.Any<UserViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task BulkCreateUser_WhenUserIsAuthenticated_ShouldReturnCreated()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/user/bulk_insert");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var usersViewModel = new List<UserViewModel>(
        [
            new() {
                Id = Guid.NewGuid(),
                Name = "Test User_A",
                Email = "test_user_a@mail.com",
                Password = "test_password",
                TelegramChatId = 123456789,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.NewGuid(),
                Name = "Test User_B",
                Email = "test_user_b@mail.com",
                Password = "test_password",
                TelegramChatId = 987654321,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        ]);

        var expectedResult = Result<int>.Success(usersViewModel.Count);

        _ = _userAppService.BulkInsertAsync(Arg.Any<ICollection<UserViewModel>>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(usersViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = int.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token));

        // Assert
        _ = _userAppService.Received(1).BulkInsertAsync(Arg.Any<ICollection<UserViewModel>>());
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        result.ShouldBeGreaterThan(0);
        _ = result.ShouldBeOfType<int>();
    }

    [TestMethod]
    public async Task BulkCreateUser_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/user/bulk_insert");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task BulkCreateUser_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/user/bulk_insert");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var usersViewModel = new List<UserViewModel>(
        [
            new() {
                Id = Guid.NewGuid(),
                Name = "Test User_A",
                Email = "test_user_a@mail.com",
                Password = "test_password",
                TelegramChatId = 123456789,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.NewGuid(),
                Name = "Test User_B",
                Email = "test_user_b@mail.com",
                Password = "test_password",
                TelegramChatId = 987654321,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        ]);

        var expectedResult = Result<int>.Failure("Service error");

        _ = _userAppService.BulkInsertAsync(Arg.Any<ICollection<UserViewModel>>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(usersViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).BulkInsertAsync(Arg.Any<ICollection<UserViewModel>>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }
    #endregion

    //##################################################################################################################

    #region MapPut Tests
    [TestMethod]
    public async Task UpdateUser_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/user/");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var userViewModel = new UserViewModel
        {
            Id = id,
            Name = "Test User_A",
            Email = "test_user_a@mail.com",
            Password = "test_password",
            TelegramChatId = 123456789,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedResult = Result<UserViewModel>.Success(userViewModel);

        _ = _userAppService.UpdateAsync(Arg.Any<UserViewModel>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(userViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<UserViewModel>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).UpdateAsync(Arg.Any<UserViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<UserViewModel>();
    }

    [TestMethod]
    public async Task UpdateUser_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/user");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task UpdateUser_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var userViewModel = new UserViewModel
        {
            Id = id,
            Name = "Test User_A",
            Email = "test_user_a@mail.com",
            Password = "test_password",
            TelegramChatId = 123456789,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedResult = Result<UserViewModel>.Failure("Service error");

        _ = _userAppService.UpdateAsync(Arg.Any<UserViewModel>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(userViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).UpdateAsync(Arg.Any<UserViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }
    #endregion

    //##################################################################################################################

    #region MapDelete Tests
    [TestMethod]
    public async Task DeleteUser_WhenUserIsAuthenticated_ShouldReturnNoContent()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/user/");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<UserViewModel>.Success(null);

        _ = _userAppService.RemoveAsync(Arg.Any<UserViewModel>()).Returns(expectedResult);
        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).RemoveAsync(Arg.Any<UserViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task DeleteUser_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/user");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task DeleteUser_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<UserViewModel>.Failure("Service error");

        _ = _userAppService.RemoveAsync(Arg.Any<UserViewModel>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationTokenSource.Token);

        // Assert
        _ = _userAppService.Received(1).RemoveAsync(Arg.Any<UserViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }
    #endregion
}
