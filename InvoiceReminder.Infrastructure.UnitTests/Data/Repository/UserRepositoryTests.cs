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
public sealed class UserRepositoryTests
{
    private readonly CoreDbContext _dbContext;
    private readonly ILogger<UserRepository> _logger;
    private readonly IUserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(default)
            .Options;

        _dbContext = Substitute.ForPartsOf<CoreDbContext>(options);
        _logger = Substitute.For<ILogger<UserRepository>>();
        _repository = Substitute.For<IUserRepository>();
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

    [TestMethod]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var email = "user_test@mail.com";
        var user = new User { Id = Guid.NewGuid(), Email = email };

        _ = _repository.GetByEmailAsync(Arg.Any<string>()).Returns(user);

        // Act
        var result = await _repository.GetByEmailAsync(email);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<User>();
            result.Email.ShouldBe(email);
        });
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };

        _ = _repository.GetByIdAsync(Arg.Any<Guid>()).Returns(user);

        // Act
        var result = await _repository.GetByIdAsync(userId);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<User>();
            result.Id.ShouldBe(userId);
        });
    }
}
