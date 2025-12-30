using Bogus;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.Domain.Enums;
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
public sealed class ScanEmailDefinitionEndpointsTests
{
    private readonly HttpClient _client;
    private readonly IAuthorizationService _authorizationService;
    private readonly IScanEmailDefinitionAppService _scanEmailDefinitionAppService;
    private readonly Faker<ScanEmailDefinitionViewModel> _scanEmailDefinitionViewModelFaker;
    private readonly Faker _faker;
    private const string basepath = "/api/scan_email";

    public TestContext TestContext { get; set; }

    public ScanEmailDefinitionEndpointsTests()
    {
        var factory = new CustomWebApplicationFactory<Program>();
        var serviceProvider = factory.Services;

        _client = factory.CreateClient();
        _authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        _scanEmailDefinitionAppService = serviceProvider.GetRequiredService<IScanEmailDefinitionAppService>();
        _faker = new Faker();

        _scanEmailDefinitionViewModelFaker = new Faker<ScanEmailDefinitionViewModel>()
            .RuleFor(s => s.Id, faker => faker.Random.Guid())
            .RuleFor(s => s.UserId, faker => faker.Random.Guid())
            .RuleFor(s => s.InvoiceType, faker => faker.PickRandom(InvoiceType.AccountInvoice, InvoiceType.BankInvoice))
            .RuleFor(s => s.Beneficiary, faker => faker.Company.CompanyName())
            .RuleFor(s => s.Description, faker => faker.Lorem.Sentence())
            .RuleFor(s => s.SenderEmailAddress, faker => faker.Internet.Email())
            .RuleFor(s => s.AttachmentFileName, faker => faker.System.FileName())
            .RuleFor(s => s.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(s => s.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
    }

    #region MapGet Tests
    [TestMethod]
    public async Task GetScanEmailDefinitions_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var scanEmailDefinitions = _scanEmailDefinitionViewModelFaker.Generate(2);
        var expectedResult = Result<IEnumerable<ScanEmailDefinitionViewModel>>.Success(scanEmailDefinitions);

        _ = _scanEmailDefinitionAppService.GetAll().Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<ScanEmailDefinitionViewModel>>(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetAll();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        result.ShouldSatisfyAllConditions(result =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<List<ScanEmailDefinitionViewModel>>();
            result.Count().ShouldBe(expectedResult.Value.Count());
        });
    }

    [TestMethod]
    public async Task GetScanEmailDefinitions_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
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
    public async Task GetAllScanEmailDefinitions_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<IEnumerable<ScanEmailDefinitionViewModel>>.Failure("Service error");

        _ = _scanEmailDefinitionAppService.GetAll().Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetAll();

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionById_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<ScanEmailDefinitionViewModel>.Success(
            _scanEmailDefinitionViewModelFaker.Clone().RuleFor(s => s.Id, id).Generate());

        _ = _scanEmailDefinitionAppService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ScanEmailDefinitionViewModel>(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ScanEmailDefinitionViewModel>();
        result.ShouldBeEquivalentTo(expectedResult.Value);
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionById_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{id}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionById_WhenUserIsAuthenticatedButServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _scanEmailDefinitionAppService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ArgumentException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionById_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _scanEmailDefinitionAppService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ApplicationException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionById_WhenUserIsAuthenticatedButNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<ScanEmailDefinitionViewModel>.Failure($"ScanEmailDefinition with id {id} not Found.");

        _ = _scanEmailDefinitionAppService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionByUserId_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/getby-userid/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var scanEmailDefinitions = _scanEmailDefinitionViewModelFaker.Generate(2);
        var expectedResult = Result<IEnumerable<ScanEmailDefinitionViewModel>>.Success(scanEmailDefinitions);

        _ = _scanEmailDefinitionAppService.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<ScanEmailDefinitionViewModel>>(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<List<ScanEmailDefinitionViewModel>>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionByUserId_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/getby-userid/{Guid.NewGuid()}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionByUserId_WhenUserIsAuthenticatedButServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/getby-userid/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _scanEmailDefinitionAppService.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ArgumentException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionByUserId_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/getby-userid/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _scanEmailDefinitionAppService.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ApplicationException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionByUserId_WhenUserIsAuthenticatedButNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/getby-userid/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<IEnumerable<ScanEmailDefinitionViewModel>>.
            Failure($"ScanEmailDefinition with user id {id} not Found.");

        _ = _scanEmailDefinitionAppService.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionBySenderEmailAddressAndUserId_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var email = _faker.Internet.Email();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{email}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<ScanEmailDefinitionViewModel>.Success(
           _scanEmailDefinitionViewModelFaker.Clone().RuleFor(s => s.SenderEmailAddress, email).Generate()
        );

        _ = _scanEmailDefinitionAppService
            .GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ScanEmailDefinitionViewModel>(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1)
            .GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ScanEmailDefinitionViewModel>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionBySenderEmailAddressAndUserId_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{Guid.NewGuid()}/{Guid.NewGuid()}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionBySenderEmailAddressAndUserId_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var email = _faker.Internet.Email();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{email}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _scanEmailDefinitionAppService
            .GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ApplicationException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1)
            .GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionBySenderEmailAddressAndUserId_WhenUserIsAuthenticatedButNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var email = _faker.Internet.Email();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{email}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<ScanEmailDefinitionViewModel>.
            Failure($"ScanEmailDefinition with user id {id} not Found.");

        _ = _scanEmailDefinitionAppService
            .GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1)
            .GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }
    #endregion

    //##################################################################################################################

    #region MapPost Tests
    [TestMethod]
    public async Task CreateScanEmailDefinition_WhenUserIsAuthenticated_ShouldReturnCreated()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var scanEmailDefinitionViewModel = _scanEmailDefinitionViewModelFaker.Generate();
        var expectedResult = Result<ScanEmailDefinitionViewModel>.Success(scanEmailDefinitionViewModel);

        _ = _scanEmailDefinitionAppService.AddAsync(Arg.Any<ScanEmailDefinitionViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(scanEmailDefinitionViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content
            .ReadFromJsonAsync<ScanEmailDefinitionViewModel>(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1)
            .AddAsync(Arg.Any<ScanEmailDefinitionViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ScanEmailDefinitionViewModel>();
    }

    [TestMethod]
    public async Task CreateScanEmailDefinition_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
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
    public async Task CreateScanEmailDefinition_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var scanEmailDefinitionViewModel = _scanEmailDefinitionViewModelFaker.Generate();
        var expectedResult = Result<ScanEmailDefinitionViewModel>.Failure("Service error");

        _ = _scanEmailDefinitionAppService.AddAsync(Arg.Any<ScanEmailDefinitionViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(scanEmailDefinitionViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1)
            .AddAsync(Arg.Any<ScanEmailDefinitionViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }
    #endregion

    //##################################################################################################################

    #region MapPut Tests
    [TestMethod]
    public async Task UpdateScanEmailDefinition_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var scanEmailDefinitionViewModel = _scanEmailDefinitionViewModelFaker.Generate();
        var expectedResult = Result<ScanEmailDefinitionViewModel>.Success(scanEmailDefinitionViewModel);

        _ = _scanEmailDefinitionAppService.UpdateAsync(Arg.Any<ScanEmailDefinitionViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(scanEmailDefinitionViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ScanEmailDefinitionViewModel>(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1)
            .UpdateAsync(Arg.Any<ScanEmailDefinitionViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ScanEmailDefinitionViewModel>();
    }

    [TestMethod]
    public async Task UpdateScanEmailDefinition_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
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
    public async Task UpdateScanEmailDefinition_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var scanEmailDefinitionViewModel = _scanEmailDefinitionViewModelFaker.Generate();
        var expectedResult = Result<ScanEmailDefinitionViewModel>.Failure("Service error");

        _ = _scanEmailDefinitionAppService.UpdateAsync(Arg.Any<ScanEmailDefinitionViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(scanEmailDefinitionViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1)
            .UpdateAsync(Arg.Any<ScanEmailDefinitionViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }
    #endregion

    //##################################################################################################################

    #region MapDelete Tests
    [TestMethod]
    public async Task DeleteScanEmailDefinition_WhenUserIsAuthenticated_ShouldReturnNoContent()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<ScanEmailDefinitionViewModel>.Success(null);

        _ = _scanEmailDefinitionAppService.RemoveAsync(Arg.Any<ScanEmailDefinitionViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1)
            .RemoveAsync(Arg.Any<ScanEmailDefinitionViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task DeleteScanEmailDefinition_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
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
    public async Task DeleteScanEmailDefinition_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<ScanEmailDefinitionViewModel>.Failure("Service error");

        _ = _scanEmailDefinitionAppService.RemoveAsync(Arg.Any<ScanEmailDefinitionViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1)
            .RemoveAsync(Arg.Any<ScanEmailDefinitionViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }
    #endregion
}
