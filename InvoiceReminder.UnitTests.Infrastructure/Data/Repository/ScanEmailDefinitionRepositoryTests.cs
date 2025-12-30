using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Data.Repository;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.UnitTests.Infrastructure.Data.Repository;

[TestClass]
public sealed class ScanEmailDefinitionRepositoryTests
{
    private readonly CoreDbContext _dbContext;
    private readonly ILogger<ScanEmailDefinitionRepository> _logger;

    public ScanEmailDefinitionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(default)
            .Options;

        _dbContext = Substitute.ForPartsOf<CoreDbContext>(options);
        _logger = Substitute.For<ILogger<ScanEmailDefinitionRepository>>();
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
}
