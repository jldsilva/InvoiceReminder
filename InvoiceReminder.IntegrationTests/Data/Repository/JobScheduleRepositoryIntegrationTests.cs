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
public sealed class JobScheduleRepositoryIntegrationTests
{
    private readonly CoreDbContext _dbContext;
    private readonly ILogger<JobScheduleRepository> _repositoryLogger;
    private readonly ILogger<UnitOfWork> _unitOfWorkLogger;
    private readonly JobScheduleRepository _repository;
    private readonly UnitOfWork _unitOfWork;

    public TestContext TestContext { get; set; }

    public JobScheduleRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options;

        _dbContext = new CoreDbContext(options);
        _repositoryLogger = Substitute.For<ILogger<JobScheduleRepository>>();
        _unitOfWorkLogger = Substitute.For<ILogger<UnitOfWork>>();
        _repository = new JobScheduleRepository(_dbContext, _repositoryLogger);
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
    public void JobScheduleRepository_ShouldBeAssignableToItsInterface_And_GenericInterface_And_GenericRepository()
    {
        // Arrange && Act
        var repository = new JobScheduleRepository(_dbContext, _repositoryLogger);

        // Assert
        repository.ShouldSatisfyAllConditions(() =>
        {
            _ = repository.ShouldBeAssignableTo<IJobScheduleRepository>();
            _ = repository.ShouldBeAssignableTo<IBaseRepository<JobSchedule>>();
            _ = repository.ShouldBeAssignableTo<BaseRepository<CoreDbContext, JobSchedule>>();

            _ = repository.ShouldNotBeNull();
            _ = repository.ShouldBeOfType<JobScheduleRepository>();
        });
    }

    #region GetByIdAsync Tests

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_JobSchedule_By_Id()
    {
        // Arrange
        var jobSchedule = await CreateAndSaveJobScheduleAsync();

        // Act
        var result = await _repository.GetByIdAsync(jobSchedule.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<JobSchedule>();
            result.Id.ShouldBe(jobSchedule.Id);
        });
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_Null_For_NonExistent_JobSchedule()
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

        var logger = Substitute.For<ILogger<JobScheduleRepository>>();

        var repository = new JobScheduleRepository(disposedContext, logger);

        // Act & Assert
        await disposedContext.DisposeAsync();

        _ = await Should.ThrowAsync<ObjectDisposedException>(
            async () => await repository.GetByIdAsync(Guid.NewGuid(), TestContext.CancellationToken)
        );
    }

    #endregion

    #region GetByUserIdAsync Tests

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Return_Empty_Collection_For_NonExistent_UserId()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByUserIdAsync(nonExistentUserId, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<List<JobSchedule>>();
            result.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Return_JobSchedules_By_UserId()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();
        var jobSchedule = TestData.JobScheduleFaker()
            .RuleFor(j => j.UserId, _ => user.Id)
            .Generate();

        _ = await _repository.AddAsync(jobSchedule, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = await _repository.GetByUserIdAsync(user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<List<JobSchedule>>();
            result.ShouldNotBeEmpty();
            result.First().UserId.ShouldBe(user.Id);
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Return_Multiple_JobSchedules_For_Same_User()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();
        var jobSchedules = TestData.JobScheduleFaker()
            .RuleFor(j => j.UserId, _ => user.Id)
            .Generate(3);

        _ = await _repository.BulkInsertAsync(jobSchedules, TestContext.CancellationToken);

        // Act
        var result = await _repository.GetByUserIdAsync(user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<List<JobSchedule>>();
            result.ShouldNotBeEmpty();
            result.Count().ShouldBe(3);
            result.ShouldAllBe(x => x.UserId == user.Id);
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Return_Only_JobSchedules_For_Specified_User()
    {
        // Arrange
        var user1 = await CreateAndSaveUserAsync();
        var user2 = await CreateAndSaveUserAsync();

        var jobSchedule1 = TestData.JobScheduleFaker()
            .RuleFor(j => j.UserId, _ => user1.Id)
            .Generate();
        var jobSchedule2 = TestData.JobScheduleFaker()
            .RuleFor(j => j.UserId, _ => user2.Id)
            .Generate();

        _ = await _repository.AddAsync(jobSchedule1, TestContext.CancellationToken);
        _ = await _repository.AddAsync(jobSchedule2, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = await _repository.GetByUserIdAsync(user1.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<List<JobSchedule>>();
            result.Count().ShouldBe(1);
            result.First().UserId.ShouldBe(user1.Id);
            result.First().Id.ShouldBe(jobSchedule1.Id);
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Throw_Exception_On_Database_Error()
    {
        // Arrange
        var disposedContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var logger = Substitute.For<ILogger<JobScheduleRepository>>();

        var repository = new JobScheduleRepository(disposedContext, logger);

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

    private async Task<User> CreateAndSaveUserAsync(User user = null)
    {
        user ??= TestData.UserFaker().Generate();
        var logger = Substitute.For<ILogger<UserRepository>>();
        var userRepository = new UserRepository(_dbContext, logger);

        _ = await userRepository.AddAsync(user, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        return user;
    }

    private async Task<JobSchedule> CreateAndSaveJobScheduleAsync(JobSchedule jobSchedule = null)
    {
        var user = await CreateAndSaveUserAsync();
        jobSchedule ??= TestData.JobScheduleFaker()
            .RuleFor(j => j.UserId, _ => user.Id)
            .Generate();

        _ = await _repository.AddAsync(jobSchedule, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        return jobSchedule;
    }

    #endregion
}
