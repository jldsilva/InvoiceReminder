using Bogus;
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
    private readonly Faker _faker;

    public TestContext TestContext { get; set; }

    public EmailAuthTokenAppServiceTests()
    {
        _repository = Substitute.For<IEmailAuthTokenRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _faker = new Faker();
    }

    private static Faker<EmailAuthToken> CreateEmailAuthTokenFaker()
    {
        return new Faker<EmailAuthToken>()
            .RuleFor(e => e.Id, faker => faker.Random.Guid())
            .RuleFor(e => e.UserId, faker => faker.Random.Guid())
            .RuleFor(e => e.AccessToken, faker => faker.Random.AlphaNumeric(128))
            .RuleFor(e => e.RefreshToken, faker => faker.Random.AlphaNumeric(128))
            .RuleFor(e => e.TokenProvider, faker => faker.PickRandom("Google", "Microsoft", "GitHub"))
            .RuleFor(e => e.NonceValue, faker => faker.Random.Hash())
            .RuleFor(e => e.AccessTokenExpiry, faker => faker.Date.Future().ToUniversalTime())
            .RuleFor(e => e.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(e => e.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
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
        var userId = _faker.Random.Guid();
        var tokenProvider = "Google";
        var emailAuthToken = CreateEmailAuthTokenFaker()
            .RuleFor(e => e.UserId, userId)
            .RuleFor(e => e.TokenProvider, tokenProvider)
            .Generate();

        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(emailAuthToken);

        // Act
        var result = await appService.GetByUserIdAsync(userId, tokenProvider, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

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
        var userId = _faker.Random.Guid();
        var tokenProvider = "Google";

        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((EmailAuthToken)null);

        // Act
        var result = await appService.GetByUserIdAsync(userId, tokenProvider, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            result.Value.ShouldBeNull();
            result.IsSuccess.ShouldBeFalse();
            result.Error.ShouldBe("EmailAuthToken not Found.");
        });
    }
}
