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
public sealed class UserPasswordRepositoryIntegrationTests
{
    private readonly CoreDbContext _dbContext;
    private readonly ILogger<UserPasswordRepository> _repositoryLogger;
    private readonly ILogger<UnitOfWork> _unitOfWorkLogger;
    private readonly UserPasswordRepository _repository;
    private readonly UnitOfWork _unitOfWork;

    public TestContext TestContext { get; set; }

    public UserPasswordRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options;

        _dbContext = new CoreDbContext(options);
        _repositoryLogger = Substitute.For<ILogger<UserPasswordRepository>>();
        _unitOfWorkLogger = Substitute.For<ILogger<UnitOfWork>>();
        _repository = new UserPasswordRepository(_dbContext, _repositoryLogger);
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
    public void UserPasswordRepository_ShouldBeAssignableToItsInterface_And_GenericInterface_And_GenericRepository()
    {
        // Arrange && Act
        var repository = new UserPasswordRepository(_dbContext, _repositoryLogger);

        // Assert
        repository.ShouldSatisfyAllConditions(() =>
        {
            _ = repository.ShouldBeAssignableTo<IUserPasswordRepository>();
            _ = repository.ShouldBeAssignableTo<IBaseRepository<UserPassword>>();
            _ = repository.ShouldBeAssignableTo<BaseRepository<CoreDbContext, UserPassword>>();

            _ = repository.ShouldNotBeNull();
            _ = repository.ShouldBeOfType<UserPasswordRepository>();
        });
    }

    #region GetByIdAsync Tests

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_UserPassword_By_Id()
    {
        // Arrange
        var userPassword = await CreateAndSaveUserPasswordAsync();

        // Act
        var result = await _repository.GetByIdAsync(userPassword.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<UserPassword>();
            result.Id.ShouldBe(userPassword.Id);
        });
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_Null_For_NonExistent_UserPassword()
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

        var logger = Substitute.For<ILogger<UserPasswordRepository>>();

        var repository = new UserPasswordRepository(disposedContext, logger);

        // Act & Assert
        await disposedContext.DisposeAsync();

        _ = await Should.ThrowAsync<ObjectDisposedException>(
            async () => await repository.GetByIdAsync(Guid.NewGuid(), TestContext.CancellationToken)
        );
    }

    #endregion

    #region GetByUserIdAsync Tests

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Return_UserPassword_By_UserId()
    {
        // Arrange
        var userPassword = await CreateAndSaveUserPasswordAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(userPassword.UserId, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<UserPassword>();
            result.UserId.ShouldBe(userPassword.UserId);
            result.PasswordHash.ShouldBe(userPassword.PasswordHash);
            result.PasswordSalt.ShouldBe(userPassword.PasswordSalt);
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Return_Null_For_NonExistent_UserId()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByUserIdAsync(nonExistentUserId, TestContext.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Throw_Exception_On_Database_Error()
    {
        // Arrange
        var disposedContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var logger = Substitute.For<ILogger<UserPasswordRepository>>();

        var repository = new UserPasswordRepository(disposedContext, logger);

        _ = logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        await disposedContext.DisposeAsync();

        _ = await Should.ThrowAsync<DataLayerException>(
            async () => await repository.GetByUserIdAsync(Guid.NewGuid(), TestContext.CancellationToken)
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

    #region ChangePasswordAsync Tests

    [TestMethod]
    public async Task ChangePasswordAsync_Should_Update_Password_Successfully()
    {
        // Arrange
        var userPassword = await CreateAndSaveUserPasswordAsync();
        var originalPasswordHash = userPassword.PasswordHash;
        var originalPasswordSalt = userPassword.PasswordSalt;
        var originalUpdatedAt = userPassword.UpdatedAt;

        userPassword.PasswordHash = "new_hash_" + Guid.NewGuid().ToString();
        userPassword.PasswordSalt = "new_salt_" + Guid.NewGuid().ToString();

        // Act
        var result = await _repository.ChangePasswordAsync(userPassword, TestContext.CancellationToken);

        // Assert
        result.ShouldBeTrue();

        var updatedUserPassword = await _repository.GetByUserIdAsync(userPassword.UserId, TestContext.CancellationToken);
        updatedUserPassword.ShouldSatisfyAllConditions(() =>
        {
            updatedUserPassword.PasswordHash.ShouldBe(userPassword.PasswordHash);
            updatedUserPassword.PasswordSalt.ShouldBe(userPassword.PasswordSalt);
            updatedUserPassword.PasswordHash.ShouldNotBe(originalPasswordHash);
            updatedUserPassword.PasswordSalt.ShouldNotBe(originalPasswordSalt);
            updatedUserPassword.UpdatedAt.ShouldBeGreaterThanOrEqualTo(originalUpdatedAt);
        });
    }

    [TestMethod]
    public async Task ChangePasswordAsync_Should_Return_False_When_UserId_NotExists()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var userPassword = new UserPassword
        {
            Id = Guid.NewGuid(),
            UserId = nonExistentUserId,
            PasswordHash = "new_hash_" + Guid.NewGuid().ToString(),
            PasswordSalt = "new_salt_" + Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.ChangePasswordAsync(userPassword, TestContext.CancellationToken);

        // Assert
        result.ShouldBeFalse();
    }

    [TestMethod]
    public async Task ChangePasswordAsync_Should_Update_Only_PasswordHash_And_PasswordSalt()
    {
        // Arrange
        var userPassword = await CreateAndSaveUserPasswordAsync();
        var originalId = userPassword.Id;
        var originalUserId = userPassword.UserId;
        var originalCreatedAt = userPassword.CreatedAt;

        userPassword.PasswordHash = "updated_hash_" + Guid.NewGuid().ToString();
        userPassword.PasswordSalt = "updated_salt_" + Guid.NewGuid().ToString();

        // Act
        var result = await _repository.ChangePasswordAsync(userPassword, TestContext.CancellationToken);

        // Assert
        result.ShouldBeTrue();

        var updatedUserPassword = await _repository.GetByUserIdAsync(userPassword.UserId, TestContext.CancellationToken);
        updatedUserPassword.ShouldSatisfyAllConditions(() =>
        {
            updatedUserPassword.Id.ShouldBe(originalId);
            updatedUserPassword.UserId.ShouldBe(originalUserId);
            var timeDifference = Math.Abs((updatedUserPassword.CreatedAt - originalCreatedAt).TotalMilliseconds);
            timeDifference.ShouldBeLessThan(1000);
        });
    }

    [TestMethod]
    public async Task ChangePasswordAsync_Should_Update_Multiple_Passwords_Independently()
    {
        // Arrange
        var userPassword1 = await CreateAndSaveUserPasswordAsync();
        var userPassword2 = await CreateAndSaveUserPasswordAsync();
        var originalHash1 = userPassword1.PasswordHash;
        var originalHash2 = userPassword2.PasswordHash;

        userPassword1.PasswordHash = "new_hash1_" + Guid.NewGuid().ToString();
        userPassword1.PasswordSalt = "new_salt1_" + Guid.NewGuid().ToString();

        // Act
        var result = await _repository.ChangePasswordAsync(userPassword1, TestContext.CancellationToken);

        // Assert
        result.ShouldBeTrue();

        var updated1 = await _repository.GetByUserIdAsync(userPassword1.UserId, TestContext.CancellationToken);
        var updated2 = await _repository.GetByUserIdAsync(userPassword2.UserId, TestContext.CancellationToken);

        updated1.ShouldSatisfyAllConditions(() =>
        {
            updated1.PasswordHash.ShouldNotBe(originalHash1);
            updated2.PasswordHash.ShouldBe(originalHash2);
        });
    }

    [TestMethod]
    public async Task ChangePasswordAsync_Should_Throw_Exception_On_Database_Error()
    {
        // Arrange
        var userPassword = await CreateAndSaveUserPasswordAsync();
        var disposedContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var logger = Substitute.For<ILogger<UserPasswordRepository>>();
        var repository = new UserPasswordRepository(disposedContext, logger);

        _ = logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        await disposedContext.DisposeAsync();

        // Act & Assert
        _ = await Should.ThrowAsync<DataLayerException>(
            async () => await repository.ChangePasswordAsync(userPassword, TestContext.CancellationToken)
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
    public async Task ChangePasswordAsync_Should_Handle_Cancellation_Request()
    {
        // Arrange
        var userPassword = await CreateAndSaveUserPasswordAsync();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = _repositoryLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _repository.ChangePasswordAsync(userPassword, cts.Token)
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
    public async Task GetByUserIdAsync_Should_Handle_Cancellation_Request()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = _repositoryLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _repository.GetByUserIdAsync(Guid.NewGuid(), cts.Token)
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

    private async Task<UserPassword> CreateAndSaveUserPasswordAsync(User user = null, UserPassword userPassword = null)
    {
        user ??= await CreateAndSaveUserAsync();
        userPassword ??= new UserPassword
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PasswordHash = "hash_" + Guid.NewGuid().ToString(),
            PasswordSalt = "salt_" + Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _ = await _repository.AddAsync(userPassword, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        return userPassword;
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
