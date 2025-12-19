using Bogus;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Data.Repository;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Enums;
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

    private static Faker<ScanEmailDefinition> CreateFaker(Action<Faker<ScanEmailDefinition>> configure = null)
    {
        var faker = new Faker<ScanEmailDefinition>()
            .RuleFor(s => s.Id, _ => Guid.NewGuid())
            .RuleFor(s => s.UserId, _ => Guid.NewGuid())
            .RuleFor(s => s.InvoiceType, f => f.PickRandom<InvoiceType>())
            .RuleFor(s => s.Beneficiary, f => f.Company.CompanyName())
            .RuleFor(s => s.Description, f => f.Lorem.Sentence())
            .RuleFor(s => s.SenderEmailAddress, f => f.Internet.Email())
            .RuleFor(s => s.AttachmentFileName, f => f.System.FileName());

        configure?.Invoke(faker);

        return faker;
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
        var beneficiary = new Faker().Company.CompanyName();
        var scanEmailDefinition = CreateFaker()
            .RuleFor(s => s.UserId, _ => userId)
            .RuleFor(s => s.Beneficiary, _ => beneficiary)
            .Generate();

        _ = _repository.GetBySenderBeneficiaryAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(scanEmailDefinition));

        // Act
        var result = await _repository.GetBySenderBeneficiaryAsync(beneficiary, userId, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<ScanEmailDefinition>();
            result.UserId.ShouldBe(userId);
            result.Beneficiary.ShouldBe(beneficiary);
        });
    }

    [TestMethod]
    public async Task GetBySenderEmailAsync_ShouldReturnScanEmailDefinition_WhenScanEmailDefinitionExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var senderEmail = new Faker().Internet.Email();
        var scanEmailDefinition = CreateFaker()
            .RuleFor(s => s.UserId, _ => userId)
            .RuleFor(s => s.SenderEmailAddress, _ => senderEmail)
            .Generate();

        _ = _repository.GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(scanEmailDefinition));

        // Act
        var result = await _repository.GetBySenderEmailAddressAsync(senderEmail, userId, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<ScanEmailDefinition>();
            result.UserId.ShouldBe(userId);
            result.SenderEmailAddress.ShouldBe(senderEmail);
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_ShouldReturnScanEmailDefinition_WhenScanEmailDefinitionExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var collection = CreateFaker()
            .RuleFor(s => s.UserId, _ => userId)
            .Generate(2);

        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<ScanEmailDefinition>>(collection));

        // Act
        var result = await _repository.GetByUserIdAsync(userId, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<List<ScanEmailDefinition>>();
            result.ShouldNotBeEmpty();
            result.ShouldContain(x => x.UserId == userId);
            result.Count().ShouldBe(2);
        });
    }
}
