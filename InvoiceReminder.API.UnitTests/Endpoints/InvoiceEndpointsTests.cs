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
public sealed class InvoiceEndpointsTests
{
    private readonly HttpClient _client;
    private readonly IAuthorizationService _authorizationService;
    private readonly IInvoiceAppService _invoiceAppService;

    public InvoiceEndpointsTests()
    {
        var factory = new CustomWebApplicationFactory<Program>();
        var serviceProvider = factory.Services;

        _client = factory.CreateClient();
        _authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        _invoiceAppService = serviceProvider.GetRequiredService<IInvoiceAppService>();
    }

    #region MapGet Tests
    [TestMethod]
    public async Task GetAllInvoices_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/invoice");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<IEnumerable<InvoiceViewModel>>.Success(
        [
            new() {
                Id = Guid.NewGuid(),
                Bank = "Banco do Brasil",
                Beneficiary = "João da Silva",
                Amount = 100.00m,
                Barcode = "12345678901234567890123456789012345678901234",
                DueDate = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.NewGuid(),
                Bank = "Banco do Brasil",
                Beneficiary = "José da Silva",
                Amount = 100.00m,
                Barcode = "12345678901234567890123456789012345678901234",
                DueDate = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        ]);

        _ = _invoiceAppService.GetAll().Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<InvoiceViewModel>>();

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
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/invoice");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetAllInvoices_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/invoice");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<IEnumerable<InvoiceViewModel>>.Failure("Service error");

        _ = _invoiceAppService.GetAll().Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

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
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/invoice/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<InvoiceViewModel>.Success(
        new()
        {
            Id = id,
            Bank = "Banco do Brasil",
            Beneficiary = "João da Silva",
            Amount = 100.00m,
            Barcode = "12345678901234567890123456789012345678901234",
            DueDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _ = _invoiceAppService.GetByIdAsync(Arg.Any<Guid>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<InvoiceViewModel>();

        // Assert
        _ = _invoiceAppService.Received(1).GetByIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<InvoiceViewModel>();
        result.ShouldBeEquivalentTo(expectedResult.Value);
    }

    [TestMethod]
    public async Task GetInvoiceById_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/invoice/{Guid.NewGuid()}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetInvoiceById_WhenUserIsAuthenticatedButServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/invoice/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _invoiceAppService.GetByIdAsync(Arg.Any<Guid>()).ThrowsAsync<ArgumentException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _invoiceAppService.Received(1).GetByIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetInvoiceById_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/invoice/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _invoiceAppService.GetByIdAsync(Arg.Any<Guid>()).ThrowsAsync<ApplicationException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _invoiceAppService.Received(1).GetByIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetInvoiceById_WhenUserIsAuthenticatedButNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/invoice/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<InvoiceViewModel>.Failure($"Invoice with id {id} not Found.");

        _ = _invoiceAppService.GetByIdAsync(Arg.Any<Guid>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        // Assert
        _ = _invoiceAppService.Received(1).GetByIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task GetInvoiceByBarcode_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var value = "12345678901234567890123456789012345678901234";
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/invoice/get_by_barcode/{value}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<InvoiceViewModel>.Success(new()
        {
            Id = Guid.NewGuid(),
            Bank = "Banco do Brasil",
            Beneficiary = "João da Silva",
            Amount = 100.00m,
            Barcode = "12345678901234567890123456789012345678901234",
            DueDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _ = _invoiceAppService.GetByBarcodeAsync(Arg.Any<string>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<InvoiceViewModel>();

        // Assert
        _ = _invoiceAppService.Received(1).GetByBarcodeAsync(Arg.Any<string>());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<InvoiceViewModel>();
    }

    [TestMethod]
    public async Task GetInvoiceByBarcode_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var value = "12345678901234567890123456789012345678901234";
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/invoice/get_by_barcode/{value}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetInvoiceByBarcode_WhenUserIsAuthenticatedButServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var value = "12345678901234567890123456789012345678901234";
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/invoice/get_by_barcode/{value}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _invoiceAppService.GetByBarcodeAsync(Arg.Any<string>()).ThrowsAsync<ArgumentException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _invoiceAppService.Received(1).GetByBarcodeAsync(Arg.Any<string>());
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetInvoiceByBarcode_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var value = "12345678901234567890123456789012345678901234";
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/invoice/get_by_barcode/{value}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _invoiceAppService.GetByBarcodeAsync(Arg.Any<string>()).ThrowsAsync<ApplicationException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _invoiceAppService.Received(1).GetByBarcodeAsync(Arg.Any<string>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetInvoiceByBarcode_WhenUserIsAuthenticatedButNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var value = "12345678901234567890123456789012345678901234";
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/invoice/get_by_barcode/{value}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<InvoiceViewModel>.Failure($"Invoice not Found.");

        _ = _invoiceAppService.GetByBarcodeAsync(Arg.Any<string>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        // Assert
        _ = _invoiceAppService.Received(1).GetByBarcodeAsync(Arg.Any<string>());
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
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/invoice");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var invoiceViewModel = new InvoiceViewModel
        {
            Id = Guid.NewGuid(),
            Bank = "Banco do Brasil",
            Beneficiary = "João da Silva",
            Amount = 100.00m,
            Barcode = "12345678901234567890123456789012345678901234",
            DueDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedResult = Result<InvoiceViewModel>.Success(invoiceViewModel);

        _ = _invoiceAppService.AddAsync(Arg.Any<InvoiceViewModel>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(invoiceViewModel);
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<InvoiceViewModel>();

        // Assert
        _ = _invoiceAppService.Received(1).AddAsync(Arg.Any<InvoiceViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<InvoiceViewModel>();
    }

    [TestMethod]
    public async Task CreateInvoice_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/invoice");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task CreateInvoice_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/invoice");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var invoiceViewModel = new InvoiceViewModel
        {
            Id = Guid.NewGuid(),
            Bank = "Banco do Brasil",
            Beneficiary = "João da Silva",
            Amount = 100.00m,
            Barcode = "12345678901234567890123456789012345678901234",
            DueDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedResult = Result<InvoiceViewModel>.Failure("Service error");

        _ = _invoiceAppService.AddAsync(Arg.Any<InvoiceViewModel>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(invoiceViewModel);
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _invoiceAppService.Received(1).AddAsync(Arg.Any<InvoiceViewModel>());
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
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/invoice/");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var invoiceViewModel = new InvoiceViewModel
        {
            Id = Guid.NewGuid(),
            Bank = "Banco do Brasil",
            Beneficiary = "João da Silva",
            Amount = 100.00m,
            Barcode = "12345678901234567890123456789012345678901234",
            DueDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedResult = Result<InvoiceViewModel>.Success(invoiceViewModel);

        _ = _invoiceAppService.UpdateAsync(Arg.Any<InvoiceViewModel>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(invoiceViewModel);
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<InvoiceViewModel>();

        // Assert
        _ = _invoiceAppService.Received(1).UpdateAsync(Arg.Any<InvoiceViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<InvoiceViewModel>();
    }

    [TestMethod]
    public async Task UpdateInvoice_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/invoice");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task UpdateInvoice_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/invoice");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var invoiceViewModel = new InvoiceViewModel
        {
            Id = Guid.NewGuid(),
            Bank = "Banco do Brasil",
            Beneficiary = "João da Silva",
            Amount = 100.00m,
            Barcode = "12345678901234567890123456789012345678901234",
            DueDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedResult = Result<InvoiceViewModel>.Failure("Service error");

        _ = _invoiceAppService.UpdateAsync(Arg.Any<InvoiceViewModel>()).Returns(expectedResult);
        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(invoiceViewModel);
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _invoiceAppService.Received(1).UpdateAsync(Arg.Any<InvoiceViewModel>());
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
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/invoice/");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<InvoiceViewModel>.Success(null);

        _ = _invoiceAppService.RemoveAsync(Arg.Any<InvoiceViewModel>()).Returns(expectedResult);
        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        // Assert
        _ = _invoiceAppService.Received(1).RemoveAsync(Arg.Any<InvoiceViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task DeleteInvoice_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/invoice");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task DeleteInvoice_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/invoice");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<InvoiceViewModel>.Failure("Service error");

        _ = _invoiceAppService.RemoveAsync(Arg.Any<InvoiceViewModel>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _invoiceAppService.Received(1).RemoveAsync(Arg.Any<InvoiceViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }
    #endregion
}
