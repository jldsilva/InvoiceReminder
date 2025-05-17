using InvoiceReminder.API.UnitTests.Factories;
using InvoiceReminder.Application.Abstractions;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
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
public sealed class ScanEmailDefinitionEndpointsTests
{
    private readonly HttpClient _client;
    private readonly IAuthorizationService _authorizationService;
    private readonly IScanEmailDefinitionAppService _scanEmailDefinitionAppService;

    public ScanEmailDefinitionEndpointsTests()
    {
        var factory = new CustomWebApplicationFactory<Program>();
        var serviceProvider = factory.Services;

        _client = factory.CreateClient();
        _authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        _scanEmailDefinitionAppService = serviceProvider.GetRequiredService<IScanEmailDefinitionAppService>();
    }

    #region MapGet Tests
    [TestMethod]
    public async Task GetScanEmailDefinitions_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/scan_email");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<IEnumerable<ScanEmailDefinitionViewModel>>.Success(
        [
            new() {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Beneficiary = "Beneficiary 1",
                Description = "Description 1",
                AttachmentFileName = "Attachment 1",
                CreatedAt = DateTime.UtcNow,
            },
            new() {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Beneficiary = "Beneficiary 2",
                Description = "Description 2",
                AttachmentFileName = "Attachment 2",
                CreatedAt = DateTime.UtcNow,
            },
        ]);

        _ = _scanEmailDefinitionAppService.GetAll().Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<ScanEmailDefinitionViewModel>>();

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
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/scan_email");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetAllScanEmailDefinitions_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/scan_email");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<IEnumerable<ScanEmailDefinitionViewModel>>.Failure("Service error");

        _ = _scanEmailDefinitionAppService.GetAll().Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

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
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/scan_email/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<ScanEmailDefinitionViewModel>.Success(
        new()
        {
            Id = id,
            UserId = Guid.NewGuid(),
            Beneficiary = "Beneficiary",
            Description = "Description",
            AttachmentFileName = "Attachment",
            SenderEmailAddress = "test@mail.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _ = _scanEmailDefinitionAppService.GetByIdAsync(Arg.Any<Guid>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ScanEmailDefinitionViewModel>();

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetByIdAsync(Arg.Any<Guid>());
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
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/scan_email/{id}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionById_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/scan_email/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _scanEmailDefinitionAppService.GetByIdAsync(Arg.Any<Guid>())
            .ThrowsAsync(new ArgumentException("Service error"));

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetByIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionById_WhenUserIsAuthenticatedButNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/scan_email/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<ScanEmailDefinitionViewModel>.Failure($"ScanEmailDefinition with id {id} not Found.");

        _ = _scanEmailDefinitionAppService.GetByIdAsync(Arg.Any<Guid>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetByIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionByUserId_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/scan_email/get_by_user_id/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<IEnumerable<ScanEmailDefinitionViewModel>>.Success(
        [
           new() {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Beneficiary = "Beneficiary 1",
                Description = "Description 1",
                AttachmentFileName = "Attachment 1",
                CreatedAt = DateTime.UtcNow,
            },
            new() {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Beneficiary = "Beneficiary 2",
                Description = "Description 2",
                AttachmentFileName = "Attachment 2",
                CreatedAt = DateTime.UtcNow,
            },
        ]);

        _ = _scanEmailDefinitionAppService.GetByUserIdAsync(Arg.Any<Guid>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<ScanEmailDefinitionViewModel>>();

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetByUserIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<List<ScanEmailDefinitionViewModel>>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionByUserId_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/scan_email/get_by_user_id/{Guid.NewGuid()}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionByUserId_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/scan_email/get_by_user_id/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _scanEmailDefinitionAppService.GetByUserIdAsync(Arg.Any<Guid>())
            .ThrowsAsync(new ArgumentException("Service error"));

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetByUserIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionByUserId_WhenUserIsAuthenticatedButNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/scan_email/get_by_user_id/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<IEnumerable<ScanEmailDefinitionViewModel>>.
            Failure($"ScanEmailDefinition with user id {id} not Found.");

        _ = _scanEmailDefinitionAppService.GetByUserIdAsync(Arg.Any<Guid>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetByUserIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionBySenderEmailAddressAndUserId_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var email = "sender_test@mail.com";
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/scan_email/{email}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<ScanEmailDefinitionViewModel>.Success(
           new()
           {
               Id = Guid.NewGuid(),
               UserId = Guid.NewGuid(),
               Beneficiary = "Beneficiary 1",
               Description = "Description 1",
               SenderEmailAddress = email,
               AttachmentFileName = "Attachment 1",
               CreatedAt = DateTime.UtcNow,
           }
        );

        _ = _scanEmailDefinitionAppService.GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ScanEmailDefinitionViewModel>();

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ScanEmailDefinitionViewModel>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionBySenderEmailAddressAndUserId_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/scan_email/{Guid.NewGuid()}/{Guid.NewGuid()}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionBySenderEmailAddressAndUserId_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var email = "sender_test@mail.com";
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/scan_email/{email}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _scanEmailDefinitionAppService.GetBySenderBeneficiaryAsync(Arg.Any<string>(), Arg.Any<Guid>())
            .ThrowsAsync(new ArgumentException("Service error"));

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetScanEmailDefinitionBySenderEmailAddressAndUserId_WhenUserIsAuthenticatedButNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var email = "sender_test@mail.com";
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/scan_email/{email}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<ScanEmailDefinitionViewModel>.
            Failure($"ScanEmailDefinition with user id {id} not Found.");

        _ = _scanEmailDefinitionAppService.GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>())
            .Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>());
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
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/scan_email");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var jobScheduleViewModel = new ScanEmailDefinitionViewModel
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Beneficiary = "Beneficiary",
            Description = "Description",
            AttachmentFileName = "Attachment",
            SenderEmailAddress = "test@mail.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedResult = Result<ScanEmailDefinitionViewModel>.Success(jobScheduleViewModel);

        _ = _scanEmailDefinitionAppService.AddAsync(Arg.Any<ScanEmailDefinitionViewModel>()).Returns(expectedResult);
        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(jobScheduleViewModel);
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ScanEmailDefinitionViewModel>();

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).AddAsync(Arg.Any<ScanEmailDefinitionViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ScanEmailDefinitionViewModel>();
    }

    [TestMethod]
    public async Task CreateScanEmailDefinition_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/scan_email");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task CreateScanEmailDefinition_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/scan_email");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var jobScheduleViewModel = new ScanEmailDefinitionViewModel
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Beneficiary = "Beneficiary",
            Description = "Description",
            AttachmentFileName = "Attachment",
            SenderEmailAddress = "test@mail.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedResult = Result<ScanEmailDefinitionViewModel>.Failure("Service error");

        _ = _scanEmailDefinitionAppService.AddAsync(Arg.Any<ScanEmailDefinitionViewModel>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(jobScheduleViewModel);
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).AddAsync(Arg.Any<ScanEmailDefinitionViewModel>());
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
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/scan_email/");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var jobScheduleViewModel = new ScanEmailDefinitionViewModel
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Beneficiary = "Beneficiary",
            Description = "Description",
            AttachmentFileName = "Attachment",
            SenderEmailAddress = "test@mail.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedResult = Result<ScanEmailDefinitionViewModel>.Success(jobScheduleViewModel);

        _ = _scanEmailDefinitionAppService.UpdateAsync(Arg.Any<ScanEmailDefinitionViewModel>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(jobScheduleViewModel);
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ScanEmailDefinitionViewModel>();

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).UpdateAsync(Arg.Any<ScanEmailDefinitionViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ScanEmailDefinitionViewModel>();
    }

    [TestMethod]
    public async Task UpdateScanEmailDefinition_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/scan_email");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task UpdateScanEmailDefinition_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/scan_email");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var jobScheduleViewModel = new ScanEmailDefinitionViewModel
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Beneficiary = "Beneficiary",
            Description = "Description",
            AttachmentFileName = "Attachment",
            SenderEmailAddress = "test@mail.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedResult = Result<ScanEmailDefinitionViewModel>.Failure("Service error");

        _ = _scanEmailDefinitionAppService.UpdateAsync(Arg.Any<ScanEmailDefinitionViewModel>()).Returns(expectedResult);
        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(jobScheduleViewModel);
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).UpdateAsync(Arg.Any<ScanEmailDefinitionViewModel>());
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
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/scan_email/");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<ScanEmailDefinitionViewModel>.Success(null);

        _ = _scanEmailDefinitionAppService.RemoveAsync(Arg.Any<ScanEmailDefinitionViewModel>()).Returns(expectedResult);
        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).RemoveAsync(Arg.Any<ScanEmailDefinitionViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task DeleteScanEmailDefinition_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/scan_email");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task DeleteScanEmailDefinition_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/scan_email");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<ScanEmailDefinitionViewModel>.Failure("Service error");

        _ = _scanEmailDefinitionAppService.RemoveAsync(Arg.Any<ScanEmailDefinitionViewModel>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _scanEmailDefinitionAppService.Received(1).RemoveAsync(Arg.Any<ScanEmailDefinitionViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }
    #endregion
}
