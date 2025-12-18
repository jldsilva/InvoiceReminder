using Bogus;
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
    private readonly Faker _faker;

    public TestContext TestContext { get; set; }

    public UserAppServiceTests()
    {
        _repository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _faker = new Faker();
    }

    private static Faker<User> CreateUserFaker()
    {
        return new Faker<User>()
            .RuleFor(u => u.Id, faker => faker.Random.Guid())
            .RuleFor(u => u.Name, faker => faker.Person.FullName)
            .RuleFor(u => u.Email, faker => faker.Internet.Email())
            .RuleFor(u => u.Password, faker => faker.Random.AlphaNumeric(32))
            .RuleFor(u => u.TelegramChatId, faker => faker.Random.Long(1000000, 9999999999))
            .RuleFor(u => u.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(u => u.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
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
        var email = _faker.Internet.Email();
        var user = CreateUserFaker()
            .RuleFor(u => u.Email, email)
            .Generate();

        _ = _repository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await appService.GetByEmailAsync(email, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

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
        var email = _faker.Internet.Email();

        _ = _repository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User)null);

        // Act
        var result = await appService.GetByEmailAsync(email, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

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
