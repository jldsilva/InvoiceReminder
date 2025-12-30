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
public sealed class InvoiceRepositoryTests
{
    private readonly CoreDbContext _dbContext;
    private readonly ILogger<InvoiceRepository> _logger;

    public InvoiceRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(default)
            .Options;

        _dbContext = Substitute.ForPartsOf<CoreDbContext>(options);
        _logger = Substitute.For<ILogger<InvoiceRepository>>();
    }

    [TestMethod]
    public void InvoiceRepository_ShouldBeAssignableToItsInterface_And_GenericInterface_And_GenericRepository()
    {
        // Arrange && Act
        var repository = new InvoiceRepository(_dbContext, _logger);

        // Assert
        repository.ShouldSatisfyAllConditions(() =>
        {
            _ = repository.ShouldBeAssignableTo<IInvoiceRepository>();
            _ = repository.ShouldBeAssignableTo<IBaseRepository<Invoice>>();
            _ = repository.ShouldBeAssignableTo<BaseRepository<CoreDbContext, Invoice>>();

            _ = repository.ShouldNotBeNull();
            _ = repository.ShouldBeOfType<InvoiceRepository>();
        });
    }
}
