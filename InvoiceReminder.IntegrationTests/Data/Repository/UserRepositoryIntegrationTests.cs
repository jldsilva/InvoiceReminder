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

    [TestCleanup]
    public void TestCleanup()
    {
        _unitOfWork?.Dispose();
        _repository?.Dispose();
        _dbContext?.Dispose();
    }

    [TestMethod]
    public void UserRepository_ShouldBeAssignableToItsInterface_And_GenericInterface_And_GenericRepository()
    {
        // Arrange && Act
        var repository = new UserRepository(_dbContext, _repositoryLogger);

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
        var user = TestData.UserFaker().Generate();
        var invoice = TestData.InvoiceFaker()
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
        var user = TestData.UserFaker().Generate();
        var emailToken = TestData.EmailAuthTokenFaker()
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

    #region UpdateBasicUserInfoAsync Tests

    [TestMethod]
    public async Task UpdateBasicUserInfoAsync_Should_Update_User_Basic_Info()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();
        var originalCreatedAt = user.CreatedAt;

        user.Name = "Updated Name";
        user.Email = "updated_email@example.com";
        user.TelegramChatId = 987654321;

        // Act
        var result = await _repository.UpdateBasicUserInfoAsync(user, TestContext.CancellationToken);

        // Assert
        result.ShouldBeTrue();

        var updatedUser = await _repository.GetByIdAsync(user.Id, TestContext.CancellationToken);
        updatedUser.ShouldSatisfyAllConditions(() =>
        {
            updatedUser.Name.ShouldBe(user.Name);
            updatedUser.Email.ShouldBe(user.Email);
            updatedUser.TelegramChatId.ShouldBe(user.TelegramChatId);
            // Allow 1 millisecond tolerance for DateTime precision differences from database
            var timeDifference = Math.Abs((updatedUser.CreatedAt - originalCreatedAt).TotalMilliseconds);
            timeDifference.ShouldBeLessThan(1);
            updatedUser.UpdatedAt.ShouldBeGreaterThan(originalCreatedAt);
        });
    }

    [TestMethod]
    public async Task UpdateBasicUserInfoAsync_Should_Return_False_When_User_NotExists()
    {
        // Arrange
        var nonExistentUser = TestData.UserFaker()
            .RuleFor(u => u.Id, _ => Guid.NewGuid())
            .Generate();

        // Act
        var result = await _repository.UpdateBasicUserInfoAsync(nonExistentUser, TestContext.CancellationToken);

        // Assert
        result.ShouldBeFalse();
    }

    [TestMethod]
    public async Task UpdateBasicUserInfoAsync_Should_Update_Only_Basic_Fields()
    {
        // Arrange
        var originalUser = await CreateAndSaveUserAsync();
        var originalInvoiceCount = originalUser.Invoices.Count;
        var originalJobScheduleCount = originalUser.JobSchedules.Count;

        var userToUpdate = TestData.UserFaker()
            .RuleFor(u => u.Id, _ => originalUser.Id)
            .RuleFor(u => u.Name, _ => "New Name")
            .RuleFor(u => u.Email, _ => "newemail@example.com")
            .RuleFor(u => u.TelegramChatId, _ => 111222333)
            .Generate();

        // Act
        var result = await _repository.UpdateBasicUserInfoAsync(userToUpdate, TestContext.CancellationToken);

        // Assert
        result.ShouldBeTrue();

        var updatedUser = await _repository.GetByIdAsync(originalUser.Id, TestContext.CancellationToken);
        updatedUser.ShouldSatisfyAllConditions(() =>
        {
            updatedUser.Name.ShouldBe("New Name");
            updatedUser.Email.ShouldBe("newemail@example.com");
            updatedUser.TelegramChatId.ShouldBe(111222333);
            updatedUser.Invoices.Count.ShouldBe(originalInvoiceCount);
            updatedUser.JobSchedules.Count.ShouldBe(originalJobScheduleCount);
        });
    }

    [TestMethod]
    public async Task UpdateBasicUserInfoAsync_Should_Update_Email_To_NewValue()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();
        var newEmail = "newuniqueemail@example.com";

        user.Email = newEmail;

        // Act
        var result = await _repository.UpdateBasicUserInfoAsync(user, TestContext.CancellationToken);

        // Assert
        result.ShouldBeTrue();

        var updatedUser = await _repository.GetByEmailAsync(newEmail, TestContext.CancellationToken);
        _ = updatedUser.ShouldNotBeNull();
        updatedUser.Id.ShouldBe(user.Id);
        updatedUser.Email.ShouldBe(newEmail);
    }

    [TestMethod]
    public async Task UpdateBasicUserInfoAsync_Should_Throw_Exception_On_Database_Error()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();
        var disposedContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var logger = Substitute.For<ILogger<UserRepository>>();
        var repository = new UserRepository(disposedContext, logger);

        _ = logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        await disposedContext.DisposeAsync();

        // Act & Assert
        _ = await Should.ThrowAsync<DataLayerException>(
            async () => await repository.UpdateBasicUserInfoAsync(user, TestContext.CancellationToken)
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
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = _repositoryLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _repository.GetByIdAsync(Guid.NewGuid(), cts.Token)
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
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = _repositoryLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _repository.GetByEmailAsync("any@mail.com", cts.Token)
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
    public async Task UpdateBasicUserInfoAsync_Should_Handle_Cancellation_Request()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = _repositoryLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _repository.UpdateBasicUserInfoAsync(user, cts.Token)
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

    private async Task<User> CreateAndSaveUserAsync(User user = null)
    {
        user ??= TestData.UserFaker().Generate();
        _ = await _repository.AddAsync(user, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        return user;
    }

    #endregion
}
