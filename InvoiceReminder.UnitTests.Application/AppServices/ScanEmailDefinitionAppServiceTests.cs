using Bogus;
using InvoiceReminder.Application.AppServices;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Enums;
using Mapster;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.UnitTests.Application.AppServices;

[TestClass]
public sealed class ScanEmailDefinitionAppServiceTests
{
    private readonly IScanEmailDefinitionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Faker _faker;

    public TestContext TestContext { get; set; }

    public ScanEmailDefinitionAppServiceTests()
    {
        _repository = Substitute.For<IScanEmailDefinitionRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _faker = new Faker();
    }

    private static Faker<ScanEmailDefinition> CreateScanEmailDefinitionFaker()
    {
        return new Faker<ScanEmailDefinition>()
            .RuleFor(s => s.Id, faker => faker.Random.Guid())
            .RuleFor(s => s.UserId, faker => faker.Random.Guid())
            .RuleFor(s => s.InvoiceType, faker => faker.PickRandom<InvoiceType>())
            .RuleFor(s => s.Beneficiary, faker => faker.Person.FullName)
            .RuleFor(s => s.Description, faker => faker.Lorem.Sentence())
            .RuleFor(s => s.SenderEmailAddress, faker => faker.Internet.Email())
            .RuleFor(s => s.AttachmentFileName, faker => faker.System.FileName("pdf"))
            .RuleFor(s => s.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(s => s.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
    }

    [TestMethod]
    public void ScanEmailDefinitionAppService_ShouldBeAssignableToItsInterface_And_GenericInterface_And_GenericAppService()
    {
        // Arrange && Act
        var appService = new ScanEmailDefinitionAppService(_repository, _unitOfWork);

        // Assert
        appService.ShouldSatisfyAllConditions(() =>
        {
            _ = appService.ShouldBeAssignableTo<IScanEmailDefinitionAppService>();
            _ = appService.ShouldNotBeNull();
            _ = appService.ShouldBeOfType<ScanEmailDefinitionAppService>();
        });
    }

    [TestMethod]
    public async Task GetBySenderBeneficiaryAsync_WhenSenderBeneficiaryExists_ShouldReturnSuccess_WithResultFound()
    {
        // Arrange
        var appService = new ScanEmailDefinitionAppService(_repository, _unitOfWork);
        var userId = _faker.Random.Guid();
        var beneficiary = _faker.Person.FullName;
        var scanEmailDefinition = CreateScanEmailDefinitionFaker()
            .RuleFor(s => s.UserId, userId)
            .RuleFor(s => s.Beneficiary, beneficiary)
            .Generate();

        _ = _repository.GetBySenderBeneficiaryAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(scanEmailDefinition);

        // Act
        var result = await appService.GetBySenderBeneficiaryAsync(beneficiary, userId, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1)
            .GetBySenderBeneficiaryAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.ShouldNotBeNull();
            _ = result.Value.ShouldNotBeNull();
            _ = result.Value.ShouldBeOfType<ScanEmailDefinitionViewModel>();
            result.Value.ShouldBeEquivalentTo(scanEmailDefinition.Adapt<ScanEmailDefinitionViewModel>());
        });
    }

    [TestMethod]
    public async Task GetBySenderBeneficiaryAsync_WhenSenderBeneficiaryNotExists_ShouldReturnFailure_WithResultNotFound()
    {
        // Arrange
        var appService = new ScanEmailDefinitionAppService(_repository, _unitOfWork);
        var beneficiary = _faker.Person.FullName;
        var userId = _faker.Random.Guid();

        _ = _repository.GetBySenderBeneficiaryAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ScanEmailDefinition)null);

        // Act
        var result = await appService.GetBySenderBeneficiaryAsync(beneficiary, userId, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1)
            .GetBySenderBeneficiaryAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            _ = result.ShouldNotBeNull();
            result.Value.ShouldBeNull();
            result.Error.ShouldBe("ScanEmailDefinition not Found.");
        });
    }

    [TestMethod]
    public async Task GetBySenderEmailAddressAsync_WhenSenderEmailAddressExists_ShouldReturnSuccess_WithResultFound()
    {
        // Arrange
        var appService = new ScanEmailDefinitionAppService(_repository, _unitOfWork);
        var userId = _faker.Random.Guid();
        var senderEmail = _faker.Internet.Email();
        var scanEmailDefinition = CreateScanEmailDefinitionFaker()
            .RuleFor(s => s.UserId, userId)
            .RuleFor(s => s.SenderEmailAddress, senderEmail)
            .Generate();

        _ = _repository.GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(scanEmailDefinition);

        // Act
        var result = await appService.GetBySenderEmailAddressAsync(senderEmail, userId, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1)
            .GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.ShouldNotBeNull();
            _ = result.Value.ShouldNotBeNull();
            _ = result.Value.ShouldBeOfType<ScanEmailDefinitionViewModel>();
            result.Value.ShouldBeEquivalentTo(scanEmailDefinition.Adapt<ScanEmailDefinitionViewModel>());
        });
    }

    [TestMethod]
    public async Task GetBySenderEmailAddressAsync_WhenSenderEmailAddressNotExists_ShouldReturnFailure_WithResultNotFound()
    {
        // Arrange
        var appService = new ScanEmailDefinitionAppService(_repository, _unitOfWork);
        var senderEmail = _faker.Internet.Email();
        var userId = _faker.Random.Guid();

        _ = _repository.GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ScanEmailDefinition)null);

        // Act
        var result = await appService.GetBySenderEmailAddressAsync(senderEmail, userId, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1)
            .GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            _ = result.ShouldNotBeNull();
            result.Value.ShouldBeNull();
            result.Error.ShouldBe("ScanEmailDefinition not Found.");
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_WhenUserIdExists_ShouldReturnSuccess_WithResultFound()
    {
        // Arrange
        var appService = new ScanEmailDefinitionAppService(_repository, _unitOfWork);
        var userId = _faker.Random.Guid();
        var scanEmailDefinitions = CreateScanEmailDefinitionFaker()
            .RuleFor(s => s.UserId, userId)
            .Generate(3)
            .ToList();

        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(scanEmailDefinitions);

        // Act
        var result = await appService.GetByUserIdAsync(userId, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.ShouldNotBeNull();
            _ = result.Value.ShouldNotBeNull();
            result.Value.ShouldAllBe(x => x.UserId == userId);
            result.Value.ShouldBeEquivalentTo(scanEmailDefinitions.Adapt<IEnumerable<ScanEmailDefinitionViewModel>>());
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_WhenUserIdNotExists_ShouldReturnFailure_WithResultNotFound()
    {
        // Arrange
        var appService = new ScanEmailDefinitionAppService(_repository, _unitOfWork);
        var userId = _faker.Random.Guid();
        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var result = await appService.GetByUserIdAsync(userId, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            _ = result.ShouldNotBeNull();
            result.Value.ShouldBeNull();
            result.Error.ShouldBe("Empty Result.");
        });
    }
}
