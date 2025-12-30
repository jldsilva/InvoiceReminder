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
public sealed class EmailAuthTokenRepositoryTests
{
    private readonly CoreDbContext _dbContext;
    private readonly ILogger<EmailAuthTokenRepository> _logger;

    public EmailAuthTokenRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(default)
            .Options;

        _dbContext = Substitute.ForPartsOf<CoreDbContext>(options);
        _logger = Substitute.For<ILogger<EmailAuthTokenRepository>>();
    }

    [TestMethod]
    public void EmailAuthTokenRepository_ShouldBeAssignableToItsInterface_And_GenericInterface_And_GenericRepository()
    {
        // Arrange && Act
        var repository = new EmailAuthTokenRepository(_dbContext, _logger);

        // Assert
        repository.ShouldSatisfyAllConditions(() =>
        {
            _ = repository.ShouldBeAssignableTo<IEmailAuthTokenRepository>();
            _ = repository.ShouldBeAssignableTo<IBaseRepository<EmailAuthToken>>();
            _ = repository.ShouldBeAssignableTo<BaseRepository<CoreDbContext, EmailAuthToken>>();

            _ = repository.ShouldNotBeNull();
            _ = repository.ShouldBeOfType<EmailAuthTokenRepository>();
        });
    }
}
