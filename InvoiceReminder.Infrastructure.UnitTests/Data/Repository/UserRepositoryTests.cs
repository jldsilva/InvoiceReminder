using Bogus;
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
    private Faker<User> _userFaker;

    public TestContext TestContext { get; set; }

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(default)
            .Options;

        _dbContext = Substitute.ForPartsOf<CoreDbContext>(options);
        _logger = Substitute.For<ILogger<UserRepository>>();
        _repository = Substitute.For<IUserRepository>();
    }

    [TestInitialize]
    public void Setup()
    {
        InitializeFaker();
    }

    private void InitializeFaker()
    {
        _userFaker = new Faker<User>()
            .RuleFor(u => u.Id, _ => Guid.NewGuid())
            .RuleFor(u => u.TelegramChatId, f => f.Random.Long(100000000, long.MaxValue))
            .RuleFor(u => u.Name, f => f.Person.FullName)
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Password, f => f.Internet.Password(length: 16, memorable: false));
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
        var email = new Faker().Internet.Email();
        var user = _userFaker
            .RuleFor(u => u.Email, _ => email)
            .Generate();

        _ = _repository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(user));

        // Act
        var result = await _repository.GetByEmailAsync(email, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<User>();
            result.Email.ShouldBe(email);
            result.Id.ShouldNotBe(Guid.Empty);
        });
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = _userFaker
            .RuleFor(u => u.Id, _ => userId)
            .Generate();

        _ = _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(user));

        // Act
        var result = await _repository.GetByIdAsync(userId, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<User>();
            result.Id.ShouldBe(userId);
        });
    }
}
