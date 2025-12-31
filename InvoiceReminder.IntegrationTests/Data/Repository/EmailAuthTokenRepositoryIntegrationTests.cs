using InvoiceReminder.Data.Exceptions;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Data.Repository;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.IntegrationTests.Data.ContainerSetup;
using InvoiceReminder.IntegrationTests.Data.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.IntegrationTests.Data.Repository;

[TestClass]
public sealed class EmailAuthTokenRepositoryIntegrationTests
{
    private readonly CoreDbContext _dbContext;
    private readonly ILogger<EmailAuthTokenRepository> _repositoryLogger;
    private readonly ILogger<UnitOfWork> _unitOfWorkLogger;
    private readonly EmailAuthTokenRepository _repository;
    private readonly UnitOfWork _unitOfWork;

    public TestContext TestContext { get; set; }

    public EmailAuthTokenRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options;

        _dbContext = new CoreDbContext(options);
        _repositoryLogger = Substitute.For<ILogger<EmailAuthTokenRepository>>();
        _unitOfWorkLogger = Substitute.For<ILogger<UnitOfWork>>();
        _repository = new EmailAuthTokenRepository(_dbContext, _repositoryLogger);
        _unitOfWork = new UnitOfWork(_dbContext, _unitOfWorkLogger);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _unitOfWork?.Dispose();
        _repository?.Dispose();
        _dbContext?.Dispose();
    }

    [TestMethod]
    public void EmailAuthTokenRepository_ShouldBeAssignableToItsInterface_And_GenericInterface_And_GenericRepository()
    {
        // Arrange && Act
        var repository = new EmailAuthTokenRepository(_dbContext, _repositoryLogger);

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

    #region GetByIdAsync Tests

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_EmailAuthToken_By_Id()
    {
        // Arrange
        var emailAuthToken = await CreateAndSaveEmailAuthTokenAsync();

        // Act
        var result = await _repository.GetByIdAsync(emailAuthToken.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<EmailAuthToken>();
            result.Id.ShouldBe(emailAuthToken.Id);
        });
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_Null_For_NonExistent_EmailAuthToken()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId, TestContext.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Throw_Exception_On_Database_Error()
    {
        // Arrange
        var disposedContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var logger = Substitute.For<ILogger<EmailAuthTokenRepository>>();

        var repository = new EmailAuthTokenRepository(disposedContext, logger);

        // Act & Assert
        await disposedContext.DisposeAsync();

        _ = await Should.ThrowAsync<Exception>(
            async () => await repository.GetByIdAsync(Guid.NewGuid(), TestContext.CancellationToken)
        );
    }

    #endregion

    #region GetByUserIdAsync Tests

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Return_EmailAuthToken_By_UserId_And_TokenProvider()
    {
        // Arrange
        var emailAuthToken = await CreateAndSaveEmailAuthTokenAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(emailAuthToken.UserId, emailAuthToken.TokenProvider, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<EmailAuthToken>();
            result.UserId.ShouldBe(emailAuthToken.UserId);
            result.TokenProvider.ShouldBe(emailAuthToken.TokenProvider);
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Return_Null_For_NonExistent_UserId()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var tokenProvider = "Google";

        // Act
        var result = await _repository.GetByUserIdAsync(nonExistentUserId, tokenProvider, TestContext.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Return_Null_For_NonExistent_TokenProvider()
    {
        // Arrange
        var emailAuthToken = await CreateAndSaveEmailAuthTokenAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(emailAuthToken.UserId, "NonExistentProvider", TestContext.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Retrieve_Correct_Token_When_Multiple_Exist()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();
        var googleToken = TestData.EmailAuthTokenFaker()
            .RuleFor(e => e.UserId, _ => user.Id)
            .RuleFor(e => e.TokenProvider, _ => "Google")
            .Generate();
        var microsoftToken = TestData.EmailAuthTokenFaker()
            .RuleFor(e => e.UserId, _ => user.Id)
            .RuleFor(e => e.TokenProvider, _ => "Microsoft")
            .Generate();

        _ = await _repository.AddAsync(googleToken, TestContext.CancellationToken);
        _ = await _repository.AddAsync(microsoftToken, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = await _repository.GetByUserIdAsync(user.Id, "Microsoft", TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<EmailAuthToken>();
            result.Id.ShouldBe(microsoftToken.Id);
            result.UserId.ShouldBe(user.Id);
            result.TokenProvider.ShouldBe("Microsoft");
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Throw_Exception_On_Database_Error()
    {
        // Arrange
        var disposedContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var logger = Substitute.For<ILogger<EmailAuthTokenRepository>>();

        var repository = new EmailAuthTokenRepository(disposedContext, logger);

        _ = logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        await disposedContext.DisposeAsync();
        _ = await Should.ThrowAsync<DataLayerException>(
            async () => await repository.GetByUserIdAsync(Guid.NewGuid(), "Google", TestContext.CancellationToken)
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
    public async Task GetByUserIdAsync_Should_Handle_Cancellation_Request()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = _repositoryLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _repository.GetByUserIdAsync(Guid.NewGuid(), "Google", cts.Token)
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

    #region Helper Methods

    private async Task<EmailAuthToken> CreateAndSaveEmailAuthTokenAsync(EmailAuthToken emailAuthToken = null)
    {
        var user = await CreateAndSaveUserAsync();
        emailAuthToken ??= TestData.EmailAuthTokenFaker()
            .RuleFor(e => e.UserId, _ => user.Id)
            .Generate();

        _ = await _repository.AddAsync(emailAuthToken, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        return emailAuthToken;
    }

    private async Task<User> CreateAndSaveUserAsync(User user = null)
    {
        user ??= TestData.UserFaker().Generate();
        var logger = Substitute.For<ILogger<UserRepository>>();
        var userRepository = new UserRepository(_dbContext, logger);

        _ = await userRepository.AddAsync(user, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        return user;
    }

    #endregion
}
