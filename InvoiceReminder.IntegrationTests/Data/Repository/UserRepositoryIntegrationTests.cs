using Bogus;
using InvoiceReminder.Data.Exceptions;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Data.Repository;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.IntegrationTests.Data.ContainerSetup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.IntegrationTests.Data.Repository;

[TestClass]
public sealed class UserRepositoryIntegrationTests
{
    private readonly CoreDbContext _dbContext;
    private readonly ILogger<UserRepository> _repositoryLogger;
    private readonly ILogger<UnitOfWork> _unitOfWorkLogger;
    private readonly UserRepository _repository;
    private readonly UnitOfWork _unitOfWork;

    public TestContext TestContext { get; set; }

    public UserRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options;

        _dbContext = new CoreDbContext(options);
        _repositoryLogger = Substitute.For<ILogger<UserRepository>>();
        _unitOfWorkLogger = Substitute.For<ILogger<UnitOfWork>>();
        _repository = new UserRepository(_dbContext, _repositoryLogger);
        _unitOfWork = new UnitOfWork(_dbContext, _unitOfWorkLogger);
    }

    #region Helper Methods

    private static Faker<EmailAuthToken> EmailAuthTokenFaker()
    {
        return new Faker<EmailAuthToken>()
            .RuleFor(e => e.Id, faker => faker.Random.Guid())
            .RuleFor(e => e.UserId, faker => faker.Random.Guid())
            .RuleFor(e => e.AccessToken, faker => faker.Random.AlphaNumeric(128))
            .RuleFor(e => e.RefreshToken, faker => faker.Random.AlphaNumeric(128))
            .RuleFor(e => e.TokenProvider, faker => faker.PickRandom("Google", "Microsoft", "GitHub"))
            .RuleFor(e => e.NonceValue, faker => faker.Random.Hash())
            .RuleFor(e => e.AccessTokenExpiry, faker => faker.Date.Future().ToUniversalTime())
            .RuleFor(e => e.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(e => e.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
    }

    private static Faker<Invoice> InvoiceFaker()
    {
        return new Faker<Invoice>()
            .RuleFor(i => i.Id, faker => faker.Random.Guid())
            .RuleFor(i => i.UserId, faker => faker.Random.Guid())
            .RuleFor(i => i.Bank, faker => faker.PickRandom(
                "Banco do Brasil",
                "Bradesco",
                "Itaú",
                "Caixa Econômica Federal",
                "Santander",
                "Safra",
                "Citibank",
                "BTG Pactual"))
            .RuleFor(i => i.Beneficiary, faker => faker.Person.FullName)
            .RuleFor(i => i.Amount, faker => faker.Finance.Amount(10, 10000))
            .RuleFor(i => i.Barcode, faker => faker.Random.AlphaNumeric(44))
            .RuleFor(i => i.DueDate, faker => faker.Date.Future().ToUniversalTime());
    }

    private static Faker<User> UserFaker()
    {
        return new Faker<User>()
            .RuleFor(u => u.Id, _ => Guid.NewGuid())
            .RuleFor(u => u.TelegramChatId, f => f.Random.Long(100000000, long.MaxValue))
            .RuleFor(u => u.Name, f => f.Person.FullName)
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Password, f => f.Internet.Password(length: 16, memorable: false));
    }

    private async Task<User> CreateAndSaveUserAsync(User user = null)
    {
        user ??= UserFaker().Generate();
        _ = await _repository.AddAsync(user, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        return user;
    }

    #endregion

    #region GetByIdAsync Tests

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_User_By_Id()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<User>();
            result.Id.ShouldBe(user.Id);
        });
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_Null_For_NonExistent_User()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId, TestContext.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Load_Related_Entities()
    {
        // Arrange
        var user = UserFaker().Generate();
        var invoice = InvoiceFaker()
            .RuleFor(u => u.UserId, _ => user.Id)
            .Generate();

        user.Invoices.Add(invoice);

        _ = await CreateAndSaveUserAsync(user);

        // Act
        var result = await _repository.GetByIdAsync(user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<User>();
            _ = result.Invoices.ShouldNotBeNull();
            result.Invoices.Count.ShouldBeGreaterThan(0);
        });
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Throw_Exception_On_Database_Error()
    {
        // Arrange
        var disposedContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var logger = Substitute.For<ILogger<UserRepository>>();

        var repository = new UserRepository(disposedContext, logger);

        _ = logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        await disposedContext.DisposeAsync();

        _ = await Should.ThrowAsync<DataLayerException>(
            async () => await repository.GetByIdAsync(Guid.NewGuid(), TestContext.CancellationToken)
        );

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    #endregion

    #region GetByEmailAsync Tests

    [TestMethod]
    public async Task GetByEmailAsync_Should_Return_User_By_Email()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();

        // Act
        var result = await _repository.GetByEmailAsync(user.Email, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<User>();
            result.Id.ShouldBe(user.Id);
        });
    }

    [TestMethod]
    public async Task GetByEmailAsync_Should_Return_Null_For_NonExistent_Email()
    {
        // Arrange
        var nonExistentEmail = "nonexistent@example.com";

        // Act
        var result = await _repository.GetByEmailAsync(nonExistentEmail, TestContext.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetByEmailAsync_Should_Find_User_With_Multiple_Related_Entities()
    {
        // Arrange
        var user = UserFaker().Generate();
        var emailToken = EmailAuthTokenFaker()
            .RuleFor(e => e.UserId, _ => user.Id)
            .Generate();

        user.EmailAuthTokens.Add(emailToken);

        _ = await CreateAndSaveUserAsync(user);

        // Act
        var result = await _repository.GetByEmailAsync(user.Email, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<User>();
            _ = result.EmailAuthTokens.ShouldNotBeNull();
            result.EmailAuthTokens.Count.ShouldBeGreaterThan(0);
        });
    }

    [TestMethod]
    public async Task GetByEmailAsync_Should_Be_Case_Sensitive()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();

        // Act
        var result = await _repository.GetByEmailAsync(
            user.Email.ToUpper(),
            TestContext.CancellationToken
        );

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetByEmailAsync_Should_Throw_Exception_On_Database_Error()
    {
        // Arrange
        var disposedContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var logger = Substitute.For<ILogger<UserRepository>>();

        var repository = new UserRepository(disposedContext, logger);

        _ = logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        await disposedContext.DisposeAsync();

        _ = await Should.ThrowAsync<DataLayerException>(
            async () => await repository.GetByEmailAsync("test@example.com", TestContext.CancellationToken)
        );

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    #endregion

    #region CancellationToken Tests

    [TestMethod]
    public async Task GetByIdAsync_Should_Handle_Cancellation_Request()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = _repositoryLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _repository.GetByIdAsync(user.Id, cts.Token)
        );

        _repositoryLogger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<OperationCanceledException>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    [TestMethod]
    public async Task GetByEmailAsync_Should_Handle_Cancellation_Request()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = _repositoryLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _repository.GetByEmailAsync(user.Email, cts.Token)
        );

        _repositoryLogger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<OperationCanceledException>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    #endregion
}
