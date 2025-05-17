using InvoiceReminder.Application.AppServices;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Entities;
using Mapster;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.Application.UnitTests.AppServices;

[TestClass]
public sealed class UserAppServiceTests
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UserAppServiceTests()
    {
        _repository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
    }

    [TestMethod]
    public void UserAppService_ShouldBeAssignableToItsInterface_And_GenericInterface_And_GenericAppService()
    {
        // Arrange && Act
        var appService = new UserAppService(_repository, _unitOfWork);

        // Assert
        appService.ShouldSatisfyAllConditions(() =>
        {
            _ = appService.ShouldBeAssignableTo<IUserAppService>();
            _ = appService.ShouldNotBeNull();
            _ = appService.ShouldBeOfType<UserAppService>();
        });
    }

    [TestMethod]
    public async Task GetByEmaildAsync_WhenUserEmailExists_ShouldReturnSuccess_WhithResultFound()
    {
        // Arrange
        var appService = new UserAppService(_repository, _unitOfWork);
        var email = "user@test.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = "Test User",
            Password = "password",
            JobSchedules = [],
            ScanEmailDefinitions = [],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _ = _repository.GetByEmailAsync(Arg.Any<string>()).Returns(user);

        // Act
        var result = await appService.GetByEmailAsync(email);

        // Assert
        _ = _repository.Received(1).GetByEmailAsync(email);

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.ShouldNotBeNull();
            _ = result.Value.ShouldNotBeNull();
            _ = result.Value.ShouldBeOfType<UserViewModel>();
            result.Value.ShouldBeEquivalentTo(user.Adapt<UserViewModel>());
        });
    }

    [TestMethod]
    public async Task GetByEmaildAsync_WhenUserEmailNotExists_ShouldReturnFailure_WhithNoResult()
    {
        // Arrange
        var appService = new UserAppService(_repository, _unitOfWork);
        var email = "not_existing@test.com";

        _ = _repository.GetByEmailAsync(Arg.Any<string>()).Returns((User)null);

        // Act
        var result = await appService.GetByEmailAsync(email);

        // Assert
        _ = _repository.Received(1).GetByEmailAsync(email);

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            _ = result.ShouldNotBeNull();
            result.Value.ShouldBeNull();
            result.Error.ShouldNotBeNullOrEmpty();
            result.Error.ShouldBe("User not Found.");
        });
    }
}
