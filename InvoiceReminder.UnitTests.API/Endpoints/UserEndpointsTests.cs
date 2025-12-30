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
using NSubstitute.ExceptionExtensions;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace InvoiceReminder.UnitTests.API.Endpoints;

[TestClass]
public sealed class UserEndpointsTests
{
    private readonly HttpClient _client;
    private readonly IAuthorizationService _authorizationService;
    private readonly IUserAppService _userAppService;
    private readonly Faker<UserViewModel> _userViewModelFaker;
    private readonly Faker _faker;
    private const string basepath = "/api/user";

    public TestContext TestContext { get; set; }

    public UserEndpointsTests()
    {
        var factory = new CustomWebApplicationFactory<Program>();
        var serviceProvider = factory.Services;

        _client = factory.CreateClient();
        _authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        _userAppService = serviceProvider.GetRequiredService<IUserAppService>();
        _faker = new Faker();

        _userViewModelFaker = new Faker<UserViewModel>()
            .RuleFor(u => u.Id, faker => faker.Random.Guid())
            .RuleFor(u => u.Name, faker => faker.Person.FullName)
            .RuleFor(u => u.Email, faker => faker.Internet.Email())
            .RuleFor(u => u.Password, faker => faker.Internet.Password())
            .RuleFor(u => u.TelegramChatId, faker => faker.Random.Long(1))
            .RuleFor(u => u.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(u => u.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
    }

    #region MapGet Tests
    [TestMethod]
    public async Task GetAllUsers_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var users = _userViewModelFaker.Generate(2);
        var expectedResult = Result<IEnumerable<UserViewModel>>.Success(users);

        _ = _userAppService.GetAll().Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<UserViewModel>>(TestContext.CancellationToken);

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
        var request = new HttpRequestMessage(HttpMethod.Get, basepath);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetAllUsers_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<IEnumerable<UserViewModel>>.Failure("Service error");

        _ = _userAppService.GetAll().Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

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
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<UserViewModel>.Success(
            _userViewModelFaker.Clone().RuleFor(u => u.Id, id).Generate());

        _ = _userAppService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<UserViewModel>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<UserViewModel>();
        result.ShouldBeEquivalentTo(expectedResult.Value);
    }

    [TestMethod]
    public async Task GetUserById_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{Guid.NewGuid()}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetUserById_WhenUserIsAuthenticatedButServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _userAppService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ArgumentException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetUserById_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _userAppService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ApplicationException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetUserById_WhenUserIsAuthenticatedButNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<UserViewModel>.Failure($"User with id {id} not Found.");

        _ = _userAppService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task GetUserByEmail_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/getby-email/{email}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<UserViewModel>.Success(
            _userViewModelFaker.Clone().RuleFor(u => u.Email, email).Generate());

        _ = _userAppService.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<UserViewModel>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<UserViewModel>();
        result.ShouldBeEquivalentTo(expectedResult.Value);
    }

    [TestMethod]
    public async Task GetUserByEmail_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/getby-email/{email}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetUserByEmail_WhenUserIsAuthenticatedButServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/getby-email/{email}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _userAppService.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ArgumentException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetUserByEmail_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/getby-email/{email}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _userAppService.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ApplicationException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetUserByEmail_WhenUserIsAuthenticatedButNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/getby-email/{email}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<UserViewModel>.Failure("User not found.");

        _ = _userAppService.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

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
        var request = new HttpRequestMessage(HttpMethod.Post, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var userViewModel = _userViewModelFaker.Generate();
        var expectedResult = Result<UserViewModel>.Success(userViewModel);

        _ = _userAppService.AddAsync(Arg.Any<UserViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(userViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<UserViewModel>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).AddAsync(Arg.Any<UserViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<UserViewModel>();
    }

    [TestMethod]
    public async Task CreateUser_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
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
    public async Task CreateUser_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var userViewModel = _userViewModelFaker.Generate();
        var expectedResult = Result<UserViewModel>.Failure("Service error");

        _ = _userAppService.AddAsync(Arg.Any<UserViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(userViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).AddAsync(Arg.Any<UserViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task BulkCreateUser_WhenUserIsAuthenticated_ShouldReturnCreated()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, $"{basepath}/bulk-insert");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var usersViewModel = _userViewModelFaker.Generate(2);
        var expectedResult = Result<int>.Success(usersViewModel.Count);

        _ = _userAppService.BulkInsertAsync(Arg.Any<ICollection<UserViewModel>>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(usersViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = int.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));

        // Assert
        _ = _userAppService.Received(1).BulkInsertAsync(Arg.Any<ICollection<UserViewModel>>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        result.ShouldBeGreaterThan(0);
        _ = result.ShouldBeOfType<int>();
    }

    [TestMethod]
    public async Task BulkCreateUser_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
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
    public async Task BulkCreateUser_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, $"{basepath}/bulk-insert");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var usersViewModel = _userViewModelFaker.Generate(2);
        var expectedResult = Result<int>.Failure("Service error");

        _ = _userAppService.BulkInsertAsync(Arg.Any<ICollection<UserViewModel>>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(usersViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).BulkInsertAsync(Arg.Any<ICollection<UserViewModel>>(), Arg.Any<CancellationToken>());

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
        var request = new HttpRequestMessage(HttpMethod.Put, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var userViewModel = _userViewModelFaker.Generate();
        var expectedResult = Result<UserViewModel>.Success(userViewModel);

        _ = _userAppService.UpdateAsync(Arg.Any<UserViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(userViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<UserViewModel>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).UpdateAsync(Arg.Any<UserViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<UserViewModel>();
    }

    [TestMethod]
    public async Task UpdateUser_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
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
    public async Task UpdateUser_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var userViewModel = _userViewModelFaker.Generate();
        var expectedResult = Result<UserViewModel>.Failure("Service error");

        _ = _userAppService.UpdateAsync(Arg.Any<UserViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(userViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).UpdateAsync(Arg.Any<UserViewModel>(), Arg.Any<CancellationToken>());

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
        var request = new HttpRequestMessage(HttpMethod.Delete, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<UserViewModel>.Success(null);

        _ = _userAppService.RemoveAsync(Arg.Any<UserViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).RemoveAsync(Arg.Any<UserViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task DeleteUser_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
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
    public async Task DeleteUser_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<UserViewModel>.Failure("Service error");

        _ = _userAppService.RemoveAsync(Arg.Any<UserViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _userAppService.Received(1).RemoveAsync(Arg.Any<UserViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }
    #endregion
}
