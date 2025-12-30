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
public sealed class UserRepositoryTests
{
    private readonly CoreDbContext _dbContext;
    private readonly ILogger<UserRepository> _logger;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(default)
            .Options;

        _dbContext = Substitute.ForPartsOf<CoreDbContext>(options);
        _logger = Substitute.For<ILogger<UserRepository>>();
    }

    [TestMethod]
    public void UserRepository_ShouldBeAssignableToItsInterface_And_GenericInterface_And_GenericRepository()
    {
        // Arrange && Act
        var repository = new UserRepository(_dbContext, _logger);

        // Assert
        repository.ShouldSatisfyAllConditions(() =>
        {
            _ = repository.ShouldBeAssignableTo<IUserRepository>();
            _ = repository.ShouldBeAssignableTo<IBaseRepository<User>>();
            _ = repository.ShouldBeAssignableTo<BaseRepository<CoreDbContext, User>>();

            _ = repository.ShouldNotBeNull();
            _ = repository.ShouldBeOfType<UserRepository>();
        });
    }
}
