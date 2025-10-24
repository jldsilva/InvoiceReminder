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
public sealed class ScanEmailDefinitionAppServiceTests
{
    private readonly IScanEmailDefinitionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TestContext TestContext { get; set; }

    public ScanEmailDefinitionAppServiceTests()
    {
        _repository = Substitute.For<IScanEmailDefinitionRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
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
        var beneficiary = "Test Beneficiary";
        var userId = Guid.NewGuid();
        var scanEmailDefinition = new ScanEmailDefinition
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AttachmentFileName = "test.pdf",
            Beneficiary = beneficiary,
            Description = "Test Description",
            SenderEmailAddress = "test@mail.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

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
        var beneficiary = "Test Beneficiary";
        var userId = Guid.NewGuid();

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
        var beneficiary = "test@email.com";
        var userId = Guid.NewGuid();
        var scanEmailDefinition = new ScanEmailDefinition
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AttachmentFileName = "test.pdf",
            Beneficiary = beneficiary,
            Description = "Test Description",
            SenderEmailAddress = "test@mail.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _ = _repository.GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(scanEmailDefinition);

        // Act
        var result = await appService.GetBySenderEmailAddressAsync(beneficiary, userId, TestContext.CancellationToken);

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
        var beneficiary = "not_existing@email.com";
        var userId = Guid.NewGuid();

        _ = _repository.GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ScanEmailDefinition)null);

        // Act
        var result = await appService.GetBySenderEmailAddressAsync(beneficiary, userId, TestContext.CancellationToken);

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
        var userId = Guid.NewGuid();
        var scanEmailDefinitions = new List<ScanEmailDefinition>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AttachmentFileName = "test.pdf",
                Beneficiary = "Test Beneficiary",
                Description = "Test Description",
                SenderEmailAddress = "test@mail.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

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
        var userId = Guid.NewGuid();
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
