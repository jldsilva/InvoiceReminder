using Bogus;
using InvoiceReminder.Data.Exceptions;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Data.Repository;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Enums;
using InvoiceReminder.IntegrationTests.Data.ContainerSetup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.IntegrationTests.Data.Repository;

[TestClass]
public sealed class ScanEmailDefinitionRepositoryIntegrationTests
{
    private readonly CoreDbContext _dbContext;
    private readonly ILogger<ScanEmailDefinitionRepository> _repositoryLogger;
    private readonly ILogger<UnitOfWork> _unitOfWorkLogger;
    private readonly ScanEmailDefinitionRepository _repository;
    private readonly UnitOfWork _unitOfWork;

    public TestContext TestContext { get; set; }

    public ScanEmailDefinitionRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options;

        _dbContext = new CoreDbContext(options);
        _repositoryLogger = Substitute.For<ILogger<ScanEmailDefinitionRepository>>(); ;
        _unitOfWorkLogger = Substitute.For<ILogger<UnitOfWork>>();
        _repository = new ScanEmailDefinitionRepository(_dbContext, _repositoryLogger);
        _unitOfWork = new UnitOfWork(_dbContext, _unitOfWorkLogger);
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

    private static Faker<ScanEmailDefinition> ScanEmailDefinitionFaker()
    {
        return new Faker<ScanEmailDefinition>()
            .RuleFor(s => s.Id, faker => faker.Random.Guid())
            .RuleFor(s => s.UserId, faker => faker.Random.Guid())
            .RuleFor(s => s.InvoiceType, faker => faker.PickRandom(InvoiceType.AccountInvoice, InvoiceType.BankInvoice))
            .RuleFor(s => s.Beneficiary, faker => faker.Person.FullName)
            .RuleFor(s => s.Description, faker => faker.Lorem.Sentence())
            .RuleFor(s => s.SenderEmailAddress, faker => faker.Internet.Email())
            .RuleFor(s => s.AttachmentFileName, faker => faker.System.FileName("pdf"))
            .RuleFor(s => s.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(s => s.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
    }

    private async Task<User> CreateAndSaveUserAsync(User user = null)
    {
        user ??= UserFaker().Generate();
        var logger = Substitute.For<ILogger<UserRepository>>();
        var userRepository = new UserRepository(_dbContext, logger);

        _ = await userRepository.AddAsync(user, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        return user;
    }

    private async Task<ScanEmailDefinition> CreateAndSaveScanEmailDefinitionAsync(ScanEmailDefinition scanEmailDefinition = null)
    {
        var user = await CreateAndSaveUserAsync();
        scanEmailDefinition ??= ScanEmailDefinitionFaker()
            .RuleFor(s => s.UserId, _ => user.Id)
            .Generate();

        _ = await _repository.AddAsync(scanEmailDefinition, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        return scanEmailDefinition;
    }

    #endregion

    #region GetByIdAsync Tests

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_ScanEmailDefinition_By_Id()
    {
        // Arrange
        var scanEmailDefinition = await CreateAndSaveScanEmailDefinitionAsync();

        // Act
        var result = await _repository.GetByIdAsync(scanEmailDefinition.Id, TestContext.CancellationToken);

        // Assert
        _ = result.ShouldNotBeNull();
        result.Id.ShouldBe(scanEmailDefinition.Id);
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_Null_For_NonExistent_ScanEmailDefinition()
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

        var logger = Substitute.For<ILogger<ScanEmailDefinitionRepository>>();

        var repository = new ScanEmailDefinitionRepository(disposedContext, logger);

        // Act & Assert
        await disposedContext.DisposeAsync();

        _ = await Should.ThrowAsync<ObjectDisposedException>(
            async () => await repository.GetByIdAsync(Guid.NewGuid(), TestContext.CancellationToken)
        );
    }

    #endregion

    #region GetBySenderEmailAddressAsync Tests

    [TestMethod]
    public async Task GetBySenderEmailAddressAsync_Should_Return_ScanEmailDefinition_By_Email_And_UserId()
    {
        // Arrange
        var scanEmailDefinition = await CreateAndSaveScanEmailDefinitionAsync();

        // Act
        var result = await _repository.GetBySenderEmailAddressAsync(scanEmailDefinition.SenderEmailAddress,
            scanEmailDefinition.UserId, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<ScanEmailDefinition>();
            result.SenderEmailAddress.ShouldBe(scanEmailDefinition.SenderEmailAddress);
            result.UserId.ShouldBe(scanEmailDefinition.UserId);
        });
    }

    [TestMethod]
    public async Task GetBySenderEmailAddressAsync_Should_Return_Null_For_NonExistent_Email()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();
        var nonExistentEmail = "nonexistent@example.com";

        // Act
        var result = await _repository.GetBySenderEmailAddressAsync(nonExistentEmail, user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetBySenderEmailAddressAsync_Should_Return_Null_For_NonExistent_UserId()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var email = "test@example.com";

        // Act
        var result = await _repository.GetBySenderEmailAddressAsync(email, nonExistentUserId, TestContext.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetBySenderEmailAddressAsync_Should_Find_Definition_With_Whitespace_In_Email()
    {
        // Arrange
        var scanEmailDefinition = await CreateAndSaveScanEmailDefinitionAsync();

        // Act
        var result = await _repository.GetBySenderEmailAddressAsync($"  {scanEmailDefinition.SenderEmailAddress}  ",
            scanEmailDefinition.UserId, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<ScanEmailDefinition>();
            result.SenderEmailAddress.ShouldBe(scanEmailDefinition.SenderEmailAddress);
        });
    }

    [TestMethod]
    public async Task GetBySenderEmailAddressAsync_Should_Retrieve_Correct_Definition_When_Multiple_Exist()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();
        var definition1 = ScanEmailDefinitionFaker()
            .RuleFor(s => s.UserId, _ => user.Id)
            .Generate();
        var definition2 = ScanEmailDefinitionFaker()
            .RuleFor(s => s.UserId, _ => user.Id)
            .Generate();

        _ = await _repository.AddAsync(definition1, TestContext.CancellationToken);
        _ = await _repository.AddAsync(definition2, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = await _repository.GetBySenderEmailAddressAsync(definition2.SenderEmailAddress,
            user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<ScanEmailDefinition>();
            result.SenderEmailAddress.ShouldBe(definition2.SenderEmailAddress);
            result.Id.ShouldBe(definition2.Id);
        });
    }

    [TestMethod]
    public async Task GetBySenderEmailAddressAsync_Should_Throw_Exception_On_Database_Error()
    {
        // Arrange
        var disposedContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var logger = Substitute.For<ILogger<ScanEmailDefinitionRepository>>();

        var repository = new ScanEmailDefinitionRepository(disposedContext, logger);

        _ = logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        await disposedContext.DisposeAsync();

        _ = await Should.ThrowAsync<DataLayerException>(
            async () => await repository.GetBySenderEmailAddressAsync("test@example.com", Guid.NewGuid(), TestContext.CancellationToken)
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

    #region GetBySenderBeneficiaryAsync Tests

    [TestMethod]
    public async Task GetBySenderBeneficiaryAsync_Should_Return_ScanEmailDefinition_By_Beneficiary_And_UserId()
    {
        // Arrange
        var scanEmailDefinition = await CreateAndSaveScanEmailDefinitionAsync();

        // Act
        var result = await _repository.GetBySenderBeneficiaryAsync(scanEmailDefinition.Beneficiary, scanEmailDefinition.UserId, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<ScanEmailDefinition>();
            result.Beneficiary.ShouldBe(scanEmailDefinition.Beneficiary);
            result.UserId.ShouldBe(scanEmailDefinition.UserId);
        });

    }

    [TestMethod]
    public async Task GetBySenderBeneficiaryAsync_Should_Return_Null_For_NonExistent_Beneficiary()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();
        var nonExistentBeneficiary = "NonExistent Company";

        // Act
        var result = await _repository.GetBySenderBeneficiaryAsync(nonExistentBeneficiary, user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetBySenderBeneficiaryAsync_Should_Return_Null_For_NonExistent_UserId()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var beneficiary = "Company Name";

        // Act
        var result = await _repository.GetBySenderBeneficiaryAsync(beneficiary, nonExistentUserId, TestContext.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetBySenderBeneficiaryAsync_Should_Find_Definition_With_Whitespace_In_Beneficiary()
    {
        // Arrange
        var scanEmailDefinition = await CreateAndSaveScanEmailDefinitionAsync();

        // Act
        var result = await _repository.GetBySenderBeneficiaryAsync($"  {scanEmailDefinition.Beneficiary}  ", scanEmailDefinition.UserId, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<ScanEmailDefinition>();
            result.Beneficiary.ShouldBe(scanEmailDefinition.Beneficiary);
        });
    }

    [TestMethod]
    public async Task GetBySenderBeneficiaryAsync_Should_Retrieve_Correct_Definition_When_Multiple_Exist()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();
        var definition1 = ScanEmailDefinitionFaker()
            .RuleFor(s => s.UserId, _ => user.Id)
            .Generate();
        var definition2 = ScanEmailDefinitionFaker()
            .RuleFor(s => s.UserId, _ => user.Id)
            .Generate();

        _ = await _repository.AddAsync(definition1, TestContext.CancellationToken);
        _ = await _repository.AddAsync(definition2, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = await _repository.GetBySenderBeneficiaryAsync(definition2.Beneficiary, user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<ScanEmailDefinition>();
            result.Beneficiary.ShouldBe(definition2.Beneficiary);
            result.Id.ShouldBe(definition2.Id);
        });
    }

    [TestMethod]
    public async Task GetBySenderBeneficiaryAsync_Should_Throw_Exception_On_Database_Error()
    {
        // Arrange
        var disposedContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var logger = Substitute.For<ILogger<ScanEmailDefinitionRepository>>();

        var repository = new ScanEmailDefinitionRepository(disposedContext, logger);

        _ = logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        await disposedContext.DisposeAsync();

        _ = await Should.ThrowAsync<DataLayerException>(
            async () => await repository.GetBySenderBeneficiaryAsync("Company Name", Guid.NewGuid(), TestContext.CancellationToken)
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
            _ = result.ShouldBeOfType<List<ScanEmailDefinition>>();
            result.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Return_ScanEmailDefinitions_By_UserId()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();
        var scanEmailDefinition = ScanEmailDefinitionFaker()
            .RuleFor(s => s.UserId, _ => user.Id)
            .Generate();

        _ = await _repository.AddAsync(scanEmailDefinition, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = await _repository.GetByUserIdAsync(user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<List<ScanEmailDefinition>>();
            result.ShouldNotBeEmpty();
            result.First().UserId.ShouldBe(user.Id);
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Return_Multiple_ScanEmailDefinitions_For_Same_User()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();
        var scanEmailDefinitions = ScanEmailDefinitionFaker()
            .RuleFor(s => s.UserId, _ => user.Id)
            .Generate(3);

        _ = await _repository.BulkInsertAsync(scanEmailDefinitions, TestContext.CancellationToken);

        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = await _repository.GetByUserIdAsync(user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<List<ScanEmailDefinition>>();
            result.Count().ShouldBe(3);
            result.ShouldAllBe(s => s.UserId == user.Id);
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Return_Only_ScanEmailDefinitions_For_Specified_User()
    {
        // Arrange
        var user1 = await CreateAndSaveUserAsync();
        var user2 = await CreateAndSaveUserAsync();

        var definition1 = ScanEmailDefinitionFaker()
            .RuleFor(s => s.UserId, _ => user1.Id)
            .Generate();
        var definition2 = ScanEmailDefinitionFaker()
            .RuleFor(s => s.UserId, _ => user2.Id)
            .Generate();

        _ = await _repository.AddAsync(definition1, TestContext.CancellationToken);
        _ = await _repository.AddAsync(definition2, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = await _repository.GetByUserIdAsync(user1.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<List<ScanEmailDefinition>>();
            result.ShouldNotBeEmpty();
            result.First().UserId.ShouldBe(user1.Id);
            result.First().Id.ShouldBe(definition1.Id);
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Throw_Exception_On_Database_Error()
    {
        // Arrange
        var disposedContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var logger = Substitute.For<ILogger<ScanEmailDefinitionRepository>>();

        var repository = new ScanEmailDefinitionRepository(disposedContext, logger);

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
    public async Task GetBySenderEmailAddressAsync_Should_Handle_Cancellation_Request()
    {
        // Arrange
        var scanEmailDefinition = await CreateAndSaveScanEmailDefinitionAsync();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = _repositoryLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _repository.GetBySenderEmailAddressAsync(scanEmailDefinition.SenderEmailAddress, scanEmailDefinition.UserId, cts.Token)
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
    public async Task GetBySenderBeneficiaryAsync_Should_Handle_Cancellation_Request()
    {
        // Arrange
        var scanEmailDefinition = await CreateAndSaveScanEmailDefinitionAsync();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = _repositoryLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _repository.GetBySenderBeneficiaryAsync(scanEmailDefinition.Beneficiary, scanEmailDefinition.UserId, cts.Token)
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
        var scanEmailDefinition = await CreateAndSaveScanEmailDefinitionAsync();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = _repositoryLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _repository.GetByUserIdAsync(scanEmailDefinition.UserId, cts.Token)
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
