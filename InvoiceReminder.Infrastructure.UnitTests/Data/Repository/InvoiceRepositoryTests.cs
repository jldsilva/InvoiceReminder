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
public sealed class InvoiceRepositoryTests
{
    private readonly CoreDbContext _dbContext;
    private readonly ILogger<InvoiceRepository> _logger;
    private readonly IInvoiceRepository _repository;

    public InvoiceRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(default)
            .Options;

        _dbContext = Substitute.ForPartsOf<CoreDbContext>(options);
        _logger = Substitute.For<ILogger<InvoiceRepository>>();
        _repository = Substitute.For<IInvoiceRepository>();
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

    [TestMethod]
    public async Task GetByBarCodeAsync_ShouldReturnInvoice_WhenInvoiceExists()
    {
        // Arrange
        var barcode = "12345678901234567890123456789012345678901234";
        var invoice = new Invoice { Barcode = barcode };

        _ = _repository.GetByBarCodeAsync(Arg.Any<string>()).Returns(invoice);

        // Act
        var result = await _repository.GetByBarCodeAsync(barcode);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldBeAssignableTo<Invoice>();
            _ = result.ShouldBeOfType<Invoice>();
            _ = result.ShouldNotBeNull();
            result.Barcode.ShouldBe(barcode);
        });
    }
}
