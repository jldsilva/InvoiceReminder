using InvoiceReminder.Application.AppServices;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Entities;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.Application.UnitTests.AppServices;

[TestClass]
public sealed class EmailAuthTokenAppServiceTests
{
    private readonly IEmailAuthTokenRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public EmailAuthTokenAppServiceTests()
    {
        _repository = Substitute.For<IEmailAuthTokenRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
    }

    [TestMethod]
    public void EmailAuthTokenAppService_ShouldBeAssignableToItsInterface_And_GenericInterface_And_GenericAppService()
    {
        // Arrange && Act
        var appService = new EmailAuthTokenAppService(_repository, _unitOfWork);

        // Assert
        appService.ShouldSatisfyAllConditions(() =>
        {
            _ = appService.ShouldBeAssignableTo<IEmailAuthTokenAppService>();
            _ = appService.ShouldNotBeNull();
            _ = appService.ShouldBeOfType<EmailAuthTokenAppService>();
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_WhenTokenExists_ShouldReturnSuccess_WithResultFound()
    {
        // Arrange
        var appService = new EmailAuthTokenAppService(_repository, _unitOfWork);
        var userId = Guid.NewGuid();
        var tokenProvider = "Google";
        var emailAuthToken = new EmailAuthToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccessToken = "access_token",
            RefreshToken = "refresh_token",
            TokenProvider = "Google",
            AccessTokenExpiry = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(emailAuthToken);

        // Act
        var result = await appService.GetByUserIdAsync(userId, tokenProvider);

        // Assert
        _ = _repository.Received(1).GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<string>());

        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.Value.ShouldNotBeNull();
            result.IsSuccess.ShouldBeTrue();
            result.Value.UserId.ShouldBe(userId);
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_WhenTokenDoesNotExist_ShouldReturnFailure_WithNotFoundMessage()
    {
        // Arrange
        var appService = new EmailAuthTokenAppService(_repository, _unitOfWork);
        var userId = Guid.NewGuid();
        var tokenProvider = "Google";

        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns((EmailAuthToken)null);

        // Act
        var result = await appService.GetByUserIdAsync(userId, tokenProvider);

        // Assert
        _ = _repository.Received(1).GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<string>());

        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            result.Value.ShouldBeNull();
            result.IsSuccess.ShouldBeFalse();
            result.Error.ShouldBe("EmailAuthToken not Found.");
        });
    }
}
