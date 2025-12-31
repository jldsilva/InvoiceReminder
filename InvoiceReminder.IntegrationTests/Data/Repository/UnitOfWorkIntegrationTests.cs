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
using System.Data;

namespace InvoiceReminder.IntegrationTests.Data.Repository;

[TestClass]
public sealed class UnitOfWorkIntegrationTests
{
    private CoreDbContext _dbContext;
    private ILogger<UnitOfWork> _logger;
    private UnitOfWork _unitOfWork;

    public TestContext TestContext { get; set; }

    [TestInitialize]
    public void TestInitialize()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options;

        _dbContext = new CoreDbContext(options);
        _logger = Substitute.For<ILogger<UnitOfWork>>();
        _unitOfWork = new UnitOfWork(_dbContext, _logger);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _unitOfWork?.Dispose();
        _dbContext?.Dispose();
    }

    #region Helper Methods

    private static Faker<User> UserFaker()
    {
        return new Faker<User>()
            .RuleFor(u => u.Id, _ => Guid.NewGuid())
            .RuleFor(u => u.TelegramChatId, f => f.Random.Long(100000000, long.MaxValue))
            .RuleFor(u => u.Name, f => f.Person.FullName)
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Password, f => f.Internet.Password(length: 16, memorable: false));
    }

    #endregion

    #region SaveChangesAsync Tests

    [TestMethod]
    public async Task SaveChangesAsync_Should_Persist_Changes_To_Database()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = _dbContext.Set<User>().Add(user);

        // Act
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Assert
        using var dbContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var result = await dbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == user.Id, TestContext.CancellationToken);

        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<User>();
            result.Id.ShouldBe(user.Id);
            result.Email.ShouldBe(user.Email);
        });
    }

    [TestMethod]
    public async Task SaveChangesAsync_Should_Persist_Multiple_Changes()
    {
        // Arrange
        var user1 = UserFaker().Generate();
        var user2 = UserFaker().Generate();
        var user3 = UserFaker().Generate();

        _ = _dbContext.Set<User>().Add(user1);
        _ = _dbContext.Set<User>().Add(user2);
        _ = _dbContext.Set<User>().Add(user3);

        // Act
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        using var dbContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var count = await dbContext.Set<User>()
            .CountAsync(u => u.Id == user1.Id || u.Id == user2.Id || u.Id == user3.Id, TestContext.CancellationToken);

        // Assert
        count.ShouldBe(3);
    }

    [TestMethod]
    public async Task SaveChangesAsync_Should_Update_Existing_Entity()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = _dbContext.Set<User>().Add(user);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        var originalEmail = user.Email;
        user.Email = "updated@example.com";
        _ = _dbContext.Set<User>().Update(user);

        // Act
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        using var dbContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var result = await dbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<User>();
            result.Email.ShouldNotBe(originalEmail);
            result.Email.ShouldBe("updated@example.com");
        });
    }

    [TestMethod]
    public async Task SaveChangesAsync_Should_Delete_Entity()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = _dbContext.Set<User>().Add(user);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        _ = _dbContext.Set<User>().Remove(user);

        // Act
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        using var dbContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var result = await dbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task SaveChangesAsync_Should_Commit_Transaction_On_Success()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = _dbContext.Set<User>().Add(user);

        // Act
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Assert - Verify data persisted by checking with a fresh context
        using var dbContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var result = await dbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == user.Id, TestContext.CancellationToken);

        _ = result.ShouldNotBeNull();
        result.Email.ShouldBe(user.Email);
    }

    [TestMethod]
    public async Task SaveChangesAsync_Should_Rollback_On_Invalid_Data()
    {
        // Arrange
        var user = UserFaker().Generate();
        user.Email = null;

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        _ = _dbContext.Set<User>().Add(user);

        // Act & Assert
        _ = await Should.ThrowAsync<DataLayerException>(
            async () => await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken)
        );

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Rolling back changes")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    [TestMethod]
    public async Task SaveChangesAsync_Should_Rollback_Transaction_On_Constraint_Violation()
    {
        // Arrange
        var user1 = UserFaker().Generate();
        var user2 = UserFaker().Generate();
        user2.Email = user1.Email;

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        _ = _dbContext.Set<User>().Add(user1);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Dispose current unit of work and create a new one
        _unitOfWork.Dispose();
        await _dbContext.DisposeAsync();

        using var newDbContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        using var newUnitOfWork = new UnitOfWork(newDbContext, _logger);

        _ = newDbContext.Set<User>().Add(user2);

        // Act & Assert
        _ = await Should.ThrowAsync<DataLayerException>(
            async () => await newUnitOfWork.SaveChangesAsync(TestContext.CancellationToken)
        );

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Rolling back changes")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    [TestMethod]
    public async Task SaveChangesAsync_Should_Open_Connection_If_Closed()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = _dbContext.Set<User>().Add(user);

        var connection = _dbContext.Database.GetDbConnection();
        await connection.CloseAsync();

        // Act
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        using var dbContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var result = await dbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == user.Id, TestContext.CancellationToken);

        // Assert
        connection.State.ShouldBe(ConnectionState.Closed);
        _ = result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task SaveChangesAsync_Should_Close_Connection_After_Save()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = _dbContext.Set<User>().Add(user);

        // Act
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Assert
        var connection = _dbContext.Database.GetDbConnection();

        connection.State.ShouldBe(ConnectionState.Closed);
    }

    #endregion

    #region CancellationToken Tests

    [TestMethod]
    public async Task SaveChangesAsync_Should_Handle_Cancellation_Request()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = _dbContext.Set<User>().Add(user);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _unitOfWork.SaveChangesAsync(cts.Token)
        );

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<OperationCanceledException>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    #endregion

    #region Dispose Tests

    [TestMethod]
    public void Dispose_Should_Release_Context_Resources()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options;

        var dbContext = new CoreDbContext(options);
        var logger = Substitute.For<ILogger<UnitOfWork>>();
        var unitOfWork = new UnitOfWork(dbContext, logger);

        // Act
        unitOfWork.Dispose();

        // Assert
        _ = Should.Throw<ObjectDisposedException>(() =>
        {
            _ = dbContext.Set<User>();
        });
    }

    [TestMethod]
    public void Dispose_Should_Be_Safe_To_Call_Multiple_Times()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options;

        var dbContext = new CoreDbContext(options);
        var logger = Substitute.For<ILogger<UnitOfWork>>();
        var unitOfWork = new UnitOfWork(dbContext, logger);

        // Act & Assert - Should not throw
        Should.NotThrow(() =>
        {
            unitOfWork.Dispose();
            unitOfWork.Dispose();
            unitOfWork.Dispose();
        });
    }

    #endregion
}
