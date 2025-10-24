using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Data.Repository;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.Infrastructure.UnitTests.Data.Repository;

[TestClass]
public sealed class ScanEmailDefinitionRepositoryTests
{
    private readonly CoreDbContext _dbContext;
    private readonly ILogger<ScanEmailDefinitionRepository> _logger;
    private readonly IScanEmailDefinitionRepository _repository;

    public TestContext TestContext { get; set; }

    public ScanEmailDefinitionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(default)
            .Options;

        _dbContext = Substitute.ForPartsOf<CoreDbContext>(options);
        _logger = Substitute.For<ILogger<ScanEmailDefinitionRepository>>();
        _repository = Substitute.For<IScanEmailDefinitionRepository>();
    }

    [TestMethod]
    public void ScanEmailDefinitionRepository_ShouldBeAssignableToItsInterface_And_GenericInterface_And_GenericRepository()
    {
        // Arrange && Act
        var repository = new ScanEmailDefinitionRepository(_dbContext, _logger);

        // Assert
        repository.ShouldSatisfyAllConditions(() =>
        {
            _ = repository.ShouldBeAssignableTo<IScanEmailDefinitionRepository>();
            _ = repository.ShouldBeAssignableTo<IBaseRepository<ScanEmailDefinition>>();
            _ = repository.ShouldBeAssignableTo<BaseRepository<CoreDbContext, ScanEmailDefinition>>();

            _ = repository.ShouldNotBeNull();
            _ = repository.ShouldBeOfType<ScanEmailDefinitionRepository>();
        });
    }

    [TestMethod]
    public async Task GetBySenderBeneficiaryAsync_ShouldReturnScanEmailDefinition_WhenScanEmailDefinitionExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var scanEmailDefinition = new ScanEmailDefinition { UserId = userId, Beneficiary = "test" };

        _ = _repository.GetBySenderBeneficiaryAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(scanEmailDefinition);

        // Act
        var result = await _repository.GetBySenderBeneficiaryAsync("test", userId, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<ScanEmailDefinition>();
            result.UserId.ShouldBe(userId);
        });
    }

    [TestMethod]
    public async Task GetBySenderEmailAsync_ShouldReturnScanEmailDefinition_WhenScanEmailDefinitionExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var scanEmailDefinition = new ScanEmailDefinition { UserId = userId, SenderEmailAddress = "test@mail.com" };

        _ = _repository.GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(scanEmailDefinition);

        // Act
        var result = await _repository.GetBySenderEmailAddressAsync("test@mail.com", userId, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<ScanEmailDefinition>();
            result.UserId.ShouldBe(userId);
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_ShouldReturnScanEmailDefinition_WhenScanEmailDefinitionExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var collection = new List<ScanEmailDefinition>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Beneficiary = "test_A" },
            new() { Id = Guid.NewGuid(), UserId = userId, Beneficiary = "test_B" }
        };

        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(collection);

        // Act
        var result = await _repository.GetByUserIdAsync(userId, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<List<ScanEmailDefinition>>();
            result.ShouldNotBeEmpty();
            result.ShouldContain(x => x.UserId == userId);
        });
    }
}
