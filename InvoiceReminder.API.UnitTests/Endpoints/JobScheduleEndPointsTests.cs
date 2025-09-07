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
public sealed class JobScheduleEndPointsTests
{
    private readonly HttpClient _client;
    private readonly IAuthorizationService _authorizationService;
    private readonly IJobScheduleAppService _jobScheduleAppService;

    public JobScheduleEndPointsTests()
    {
        var factory = new CustomWebApplicationFactory<Program>();
        var serviceProvider = factory.Services;

        _client = factory.CreateClient();
        _authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        _jobScheduleAppService = serviceProvider.GetRequiredService<IJobScheduleAppService>();
    }

    #region MapGet Tests
    [TestMethod]
    public async Task GetAllJobSchedules_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/job_schedule");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<IEnumerable<JobScheduleViewModel>>.Success(
        [
            new() {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                CronExpression = "0 0 * * *",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                CronExpression = "0 0 * 1 1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        ]);

        _ = _jobScheduleAppService.GetAll().Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<JobScheduleViewModel>>();

        // Assert
        _ = _jobScheduleAppService.Received(1).GetAll();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<List<JobScheduleViewModel>>();
        result.Count().ShouldBe(expectedResult.Value.Count());
    }

    [TestMethod]
    public async Task GetAllJobSchedules_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/job_schedule");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetAllJobSchedules_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/job_schedule");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<IEnumerable<JobScheduleViewModel>>.Failure("Service error");

        _ = _jobScheduleAppService.GetAll().Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _jobScheduleAppService.Received(1).GetAll();
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetJobScheduleById_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/job_schedule/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<JobScheduleViewModel>.Success(
        new()
        {
            Id = id,
            UserId = Guid.NewGuid(),
            CronExpression = "0 0 * 1 2",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _ = _jobScheduleAppService.GetByIdAsync(Arg.Any<Guid>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<JobScheduleViewModel>();

        // Assert
        _ = _jobScheduleAppService.Received(1).GetByIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<JobScheduleViewModel>();
        result.ShouldBeEquivalentTo(expectedResult.Value);
    }

    [TestMethod]
    public async Task GetJobScheduleById_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/job_schedule/{Guid.NewGuid()}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetJobScheduleById_WhenUserIsAuthenticatedButServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/job_schedule/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _jobScheduleAppService.GetByIdAsync(Arg.Any<Guid>()).ThrowsAsync(new ArgumentException("Service error"));

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _jobScheduleAppService.Received(1).GetByIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetJobScheduleById_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/job_schedule/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _jobScheduleAppService.GetByIdAsync(Arg.Any<Guid>()).ThrowsAsync(new ApplicationException("Service error"));

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _jobScheduleAppService.Received(1).GetByIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetJobScheduleById_WhenUserIsAuthenticatedButNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/job_schedule/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<JobScheduleViewModel>.Failure($"JobSchedule with id {id} not Found.");

        _ = _jobScheduleAppService.GetByIdAsync(Arg.Any<Guid>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        // Assert
        _ = _jobScheduleAppService.Received(1).GetByIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task GetJobScheduleByUserId_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/job_schedule/get_by_user_id/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<IEnumerable<JobScheduleViewModel>>.Success(
        [
            new() {
                Id = Guid.NewGuid(),
                UserId = id,
                CronExpression = "0 0 * * *",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.NewGuid(),
                UserId = id,
                CronExpression = "0 0 * 1 1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        ]);

        _ = _jobScheduleAppService.GetByUserIdAsync(Arg.Any<Guid>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<JobScheduleViewModel>>();

        // Assert
        _ = _jobScheduleAppService.Received(1).GetByUserIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<List<JobScheduleViewModel>>();
    }

    [TestMethod]
    public async Task GetJobScheduleByUserId_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/job_schedule/get_by_user_id/{Guid.NewGuid()}");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GetJobScheduleByUserId_WhenUserIsAuthenticatedButServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/job_schedule/get_by_user_id/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _jobScheduleAppService.GetByUserIdAsync(Arg.Any<Guid>()).ThrowsAsync<ArgumentException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _jobScheduleAppService.Received(1).GetByUserIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetJobScheduleByUserId_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/job_schedule/get_by_user_id/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        _ = _jobScheduleAppService.GetByUserIdAsync(Arg.Any<Guid>()).ThrowsAsync<ApplicationException>();

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _jobScheduleAppService.Received(1).GetByUserIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }

    [TestMethod]
    public async Task GetJobScheduleByUserId_WhenUserIsAuthenticatedButNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/job_schedule/get_by_user_id/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<IEnumerable<JobScheduleViewModel>>.Failure($"JobSchedule with user id {id} not Found.");

        _ = _jobScheduleAppService.GetByUserIdAsync(Arg.Any<Guid>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        // Assert
        _ = _jobScheduleAppService.Received(1).GetByUserIdAsync(Arg.Any<Guid>());
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }
    #endregion

    //##################################################################################################################

    #region MapPost Tests
    [TestMethod]
    public async Task CreateJobSchedule_WhenUserIsAuthenticated_ShouldReturnCreated()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/job_schedule");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var jobScheduleViewModel = new JobScheduleViewModel
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CronExpression = "0 0 * * *",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedResult = Result<JobScheduleViewModel>.Success(jobScheduleViewModel);

        _ = _jobScheduleAppService.AddNewJobAsync(Arg.Any<JobScheduleViewModel>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(jobScheduleViewModel);
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<JobScheduleViewModel>();

        // Assert
        _ = _jobScheduleAppService.Received(1).AddNewJobAsync(Arg.Any<JobScheduleViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<JobScheduleViewModel>();
    }

    [TestMethod]
    public async Task CreateJobSchedule_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/job_schedule");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task CreateJobSchedule_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/job_schedule");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var jobScheduleViewModel = new JobScheduleViewModel
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CronExpression = "0 0 * * *",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedResult = Result<JobScheduleViewModel>.Failure("Service error");

        _ = _jobScheduleAppService.AddNewJobAsync(Arg.Any<JobScheduleViewModel>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(jobScheduleViewModel);
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _jobScheduleAppService.Received(1).AddNewJobAsync(Arg.Any<JobScheduleViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }
    #endregion

    //##################################################################################################################

    #region MapPut Tests
    [TestMethod]
    public async Task UpdateJobSchedule_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/job_schedule/");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var jobScheduleViewModel = new JobScheduleViewModel
        {
            Id = id,
            UserId = Guid.NewGuid(),
            CronExpression = "0 0 * * *",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedResult = Result<JobScheduleViewModel>.Success(jobScheduleViewModel);

        _ = _jobScheduleAppService.UpdateAsync(Arg.Any<JobScheduleViewModel>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(jobScheduleViewModel);
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<JobScheduleViewModel>();

        // Assert
        _ = _jobScheduleAppService.Received(1).UpdateAsync(Arg.Any<JobScheduleViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<JobScheduleViewModel>();
    }

    [TestMethod]
    public async Task UpdateJobSchedule_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/job_schedule");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task UpdateJobSchedule_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/job_schedule");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var jobScheduleViewModel = new JobScheduleViewModel
        {
            Id = id,
            UserId = Guid.NewGuid(),
            CronExpression = "0 0 * * *",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedResult = Result<JobScheduleViewModel>.Failure("Service error");

        _ = _jobScheduleAppService.UpdateAsync(Arg.Any<JobScheduleViewModel>()).Returns(expectedResult);
        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        request.Content = JsonContent.Create(jobScheduleViewModel);
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _jobScheduleAppService.Received(1).UpdateAsync(Arg.Any<JobScheduleViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }
    #endregion

    //##################################################################################################################

    #region MapDelete Tests
    [TestMethod]
    public async Task DeleteJobSchedule_WhenUserIsAuthenticated_ShouldReturnNoContent()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/job_schedule/");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<JobScheduleViewModel>.Success(null);

        _ = _jobScheduleAppService.RemoveAsync(Arg.Any<JobScheduleViewModel>()).Returns(expectedResult);
        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        // Assert
        _ = _jobScheduleAppService.Received(1).RemoveAsync(Arg.Any<JobScheduleViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<string>();
    }

    [TestMethod]
    public async Task DeleteJobSchedule_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/job_schedule");

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task DeleteJobSchedule_WhenUserIsAuthenticatedButServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/job_schedule");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test_token");

        var expectedResult = Result<JobScheduleViewModel>.Failure("Service error");

        _ = _jobScheduleAppService.RemoveAsync(Arg.Any<JobScheduleViewModel>()).Returns(expectedResult);

        _ = _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        _ = _jobScheduleAppService.Received(1).RemoveAsync(Arg.Any<JobScheduleViewModel>());
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _ = result.ShouldNotBeNull();
        _ = result.ShouldBeOfType<ProblemDetails>();
    }
    #endregion
}
