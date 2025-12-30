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
public sealed class InvoiceEndpointsTests
{
    private readonly HttpClient _client;
    private readonly IAuthorizationService _authorizationService;
    private readonly IInvoiceAppService _invoiceAppService;
    private readonly Faker<InvoiceViewModel> _invoiceViewModelFaker;
    private readonly Faker _faker;
    private const string basepath = "/api/invoice";

    public TestContext TestContext { get; set; }

    public InvoiceEndpointsTests()
    {
        var factory = new CustomWebApplicationFactory<Program>();
        var serviceProvider = factory.Services;

        _client = factory.CreateClient();
        _authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        _invoiceAppService = serviceProvider.GetRequiredService<IInvoiceAppService>();
        _faker = new Faker();

        _invoiceViewModelFaker = new Faker<InvoiceViewModel>()
            .RuleFor(i => i.Id, faker => faker.Random.Guid())
            .RuleFor(i => i.UserId, faker => faker.Random.Guid())
            .RuleFor(i => i.Bank, faker => faker.PickRandom(
                "Banco do Brasil",
                "Bradesco",
                "Itaú",
                "Caixa Econômica Federal",
                "Santander",
                "Safra",
                "Citibank",
                "BTG Pactual"))
            .RuleFor(i => i.Beneficiary, faker => faker.Person.FullName)
            .RuleFor(i => i.Amount, faker => faker.Finance.Amount(10, 10000))
            .RuleFor(i => i.Barcode, faker => faker.Random.AlphaNumeric(44))
            .RuleFor(i => i.DueDate, faker => faker.Date.Future().ToUniversalTime())
            .RuleFor(i => i.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(i => i.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
    }

    #region MapGet Tests
    [TestMethod]
    public async Task GetAllInvoices_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var invoices = _invoiceViewModelFaker.Generate(2);
        var expectedResult = Result<IEnumerable<InvoiceViewModel>>.Success(invoices);

        _ = _invoiceAppService.GetAll().Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<InvoiceViewModel>>(TestContext.CancellationToken);

        // Assert
        _ = _invoiceAppService.Received(1).GetAll();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<List<InvoiceViewModel>>();
        result.Count().ShouldBe(expectedResult.Value.Count());
    }

    [TestMethod]
    public async Task GetAllInvoices_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
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
    public async Task GetAllInvoices_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<IEnumerable<InvoiceViewModel>>.Failure("Service error");

        _ = _invoiceAppService.GetAll().Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _invoiceAppService.Received(1).GetAll();

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetInvoiceById_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<InvoiceViewModel>.Success(
            _invoiceViewModelFaker.Clone().RuleFor(i => i.Id, id).Generate());

        _ = _invoiceAppService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<InvoiceViewModel>(TestContext.CancellationToken);

        // Assert
        _ = _invoiceAppService.Received(1).GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<InvoiceViewModel>();
        result.ShouldBeEquivalentTo(expectedResult.Value);
    }

    [TestMethod]
    public async Task GetInvoiceById_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
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
    public async Task GetInvoiceById_WhenUserIsAuthenticatedButServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _invoiceAppService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ArgumentException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _invoiceAppService.Received(1).GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetInvoiceById_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _invoiceAppService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ApplicationException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _invoiceAppService.Received(1).GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetInvoiceById_WhenUserIsAuthenticatedButNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<InvoiceViewModel>.Failure($"Invoice with id {id} not Found.");

        _ = _invoiceAppService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Assert
        _ = _invoiceAppService.Received(1).GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task GetInvoiceByBarcode_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var barcode = _faker.Random.AlphaNumeric(44);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/getby-barcode/{barcode}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<InvoiceViewModel>.Success(
            _invoiceViewModelFaker.Clone().RuleFor(i => i.Barcode, barcode).Generate());

        _ = _invoiceAppService.GetByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<InvoiceViewModel>(TestContext.CancellationToken);

        // Assert
        _ = _invoiceAppService.Received(1).GetByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<InvoiceViewModel>();
    }

    [TestMethod]
    public async Task GetInvoiceByBarcode_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var barcode = _faker.Random.AlphaNumeric(44);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/getby-barcode/{barcode}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetInvoiceByBarcode_WhenUserIsAuthenticatedButServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var barcode = _faker.Random.AlphaNumeric(44);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/getby-barcode/{barcode}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _invoiceAppService.GetByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ArgumentException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _invoiceAppService.Received(1).GetByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetInvoiceByBarcode_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var barcode = _faker.Random.AlphaNumeric(44);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/getby-barcode/{barcode}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _invoiceAppService.GetByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<ApplicationException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _invoiceAppService.Received(1).GetByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetInvoiceByBarcode_WhenUserIsAuthenticatedButNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var barcode = _faker.Random.AlphaNumeric(44);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{basepath}/getby-barcode/{barcode}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<InvoiceViewModel>.Failure($"Invoice not Found.");

        _ = _invoiceAppService.GetByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Assert
        _ = _invoiceAppService.Received(1).GetByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }
    #endregion

    //##################################################################################################################

    #region MapPost Tests
    [TestMethod]
    public async Task CreateInvoice_WhenUserIsAuthenticated_ShouldReturnCreated()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var invoiceViewModel = _invoiceViewModelFaker.Generate();
        var expectedResult = Result<InvoiceViewModel>.Success(invoiceViewModel);

        _ = _invoiceAppService.AddAsync(Arg.Any<InvoiceViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(invoiceViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<InvoiceViewModel>(TestContext.CancellationToken);

        // Assert
        _ = _invoiceAppService.Received(1).AddAsync(Arg.Any<InvoiceViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<InvoiceViewModel>();
    }

    [TestMethod]
    public async Task CreateInvoice_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
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
    public async Task CreateInvoice_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var invoiceViewModel = _invoiceViewModelFaker.Generate();
        var expectedResult = Result<InvoiceViewModel>.Failure("Service error");

        _ = _invoiceAppService.AddAsync(Arg.Any<InvoiceViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(invoiceViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _invoiceAppService.Received(1).AddAsync(Arg.Any<InvoiceViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }
    #endregion

    //##################################################################################################################

    #region MapPut Tests
    [TestMethod]
    public async Task UpdateInvoice_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var invoiceViewModel = _invoiceViewModelFaker.Generate();
        var expectedResult = Result<InvoiceViewModel>.Success(invoiceViewModel);

        _ = _invoiceAppService.UpdateAsync(Arg.Any<InvoiceViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(invoiceViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<InvoiceViewModel>(TestContext.CancellationToken);

        // Assert
        _ = _invoiceAppService.Received(1).UpdateAsync(Arg.Any<InvoiceViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<InvoiceViewModel>();
    }

    [TestMethod]
    public async Task UpdateInvoice_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
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
    public async Task UpdateInvoice_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var invoiceViewModel = _invoiceViewModelFaker.Generate();
        var expectedResult = Result<InvoiceViewModel>.Failure("Service error");

        _ = _invoiceAppService.UpdateAsync(Arg.Any<InvoiceViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(invoiceViewModel);
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _invoiceAppService.Received(1).UpdateAsync(Arg.Any<InvoiceViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }
    #endregion

    //##################################################################################################################

    #region MapDelete Tests
    [TestMethod]
    public async Task DeleteInvoice_WhenUserIsAuthenticated_ShouldReturnNoContent()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<InvoiceViewModel>.Success(null);

        _ = _invoiceAppService.RemoveAsync(Arg.Any<InvoiceViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Assert
        _ = _invoiceAppService.Received(1).RemoveAsync(Arg.Any<InvoiceViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task DeleteInvoice_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
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
    public async Task DeleteInvoice_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, basepath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<InvoiceViewModel>.Failure("Service error");

        _ = _invoiceAppService.RemoveAsync(Arg.Any<InvoiceViewModel>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request, TestContext.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.CancellationToken);

        // Assert
        _ = _invoiceAppService.Received(1).RemoveAsync(Arg.Any<InvoiceViewModel>(), Arg.Any<CancellationToken>());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }
    #endregion
}
