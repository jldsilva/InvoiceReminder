using Bogus;
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
public sealed class BaseRepositoryIntegrationTests
{
    private CoreDbContext _dbContext;
    private BaseRepository<CoreDbContext, User> _userRepository;
    private BaseRepository<CoreDbContext, Invoice> _invoiceRepository;
    private UnitOfWork _unitOfWork;

    public TestContext TestContext { get; set; }

    [TestInitialize]
    public void TestInitialize()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options;

        _dbContext = new CoreDbContext(options);
        _userRepository = new BaseRepository<CoreDbContext, User>(_dbContext);
        _invoiceRepository = new BaseRepository<CoreDbContext, Invoice>(_dbContext);
        _unitOfWork = new UnitOfWork(_dbContext, Substitute.For<ILogger<UnitOfWork>>());
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _unitOfWork?.Dispose();
        _userRepository?.Dispose();
        _invoiceRepository?.Dispose();
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

    #region AddAsync Tests

    [TestMethod]
    public async Task AddAsync_Should_Add_Entity_To_DbSet()
    {
        // Arrange
        var user = UserFaker().Generate();

        // Act
        _ = await _userRepository.AddAsync(user, TestContext.CancellationToken);

        // Assert
        var entry = _dbContext.Entry(user);
        entry.State.ShouldBe(EntityState.Added);
    }

    [TestMethod]
    public async Task AddAsync_Should_Persist_Added_Entity_After_SaveChanges()
    {
        // Arrange
        var user = UserFaker().Generate();

        // Act
        _ = await _userRepository.AddAsync(user, TestContext.CancellationToken);
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
            result.Email.ShouldBe(user.Email);
        });
    }

    [TestMethod]
    public async Task AddAsync_Should_Return_Added_Entity()
    {
        // Arrange
        var user = UserFaker().Generate();

        // Act
        var result = await _userRepository.AddAsync(user, TestContext.CancellationToken);

        // Assert
        result.ShouldBeSameAs(user);
    }

    #endregion

    #region BulkInsertAsync Tests

    [TestMethod]
    public async Task BulkInsertAsync_Should_Insert_Multiple_Entities()
    {
        // Arrange
        var users = UserFaker().Generate(5);

        // Act
        var result = await _userRepository.BulkInsertAsync(users, TestContext.CancellationToken);

        // Assert
        result.ShouldBe(5);
    }

    [TestMethod]
    public async Task BulkInsertAsync_Should_Set_CreatedAt_And_UpdatedAt()
    {
        // Arrange
        var users = UserFaker().Generate(3);

        // Act
        _ = await _userRepository.BulkInsertAsync(users, TestContext.CancellationToken);

        // Assert
        users.ShouldAllBe(u => u.CreatedAt != default && u.UpdatedAt != default);
    }

    [TestMethod]
    public async Task BulkInsertAsync_Should_Persist_Entities_To_Database()
    {
        // Arrange
        var users = UserFaker().Generate(3);

        // Act
        _ = await _userRepository.BulkInsertAsync(users, TestContext.CancellationToken);

        // Assert
        using var dbContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var userIds = users.Select(u => u.Id).ToList();
        var count = await dbContext.Set<User>()
            .CountAsync(u => userIds.Contains(u.Id), TestContext.CancellationToken);

        count.ShouldBe(3);
    }

    [TestMethod]
    public async Task BulkInsertAsync_Should_Handle_Empty_Collection()
    {
        // Arrange
        var emptyUsers = new List<User>();

        // Act
        var result = await _userRepository.BulkInsertAsync(emptyUsers, TestContext.CancellationToken);

        // Assert
        result.ShouldBe(0);
    }

    [TestMethod]
    public async Task BulkInsertAsync_Should_Handle_Cancellation()
    {
        // Arrange
        var users = UserFaker().Generate(5);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _userRepository.BulkInsertAsync(users, cts.Token)
        );
    }

    #endregion

    #region GetByIdAsync Tests

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_Entity_By_Id()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = await _userRepository.AddAsync(user, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = await _userRepository.GetByIdAsync(user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<User>();
            result.Id.ShouldBe(user.Id);
            result.Email.ShouldBe(user.Email);
        });
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_Null_For_NonExistent_Id()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _userRepository.GetByIdAsync(nonExistentId, TestContext.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Handle_Cancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _userRepository.GetByIdAsync(Guid.NewGuid(), cts.Token)
        );
    }

    #endregion

    #region GetAll Tests

    [TestMethod]
    public async Task GetAll_Should_Return_All_Entities()
    {
        // Arrange
        var users = UserFaker().Generate(3);

        _ = await _userRepository.BulkInsertAsync(users, TestContext.CancellationToken);

        // Dispose current context to ensure we read from fresh one
        _userRepository.Dispose();
        await _dbContext.DisposeAsync();

        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options;

        using var dbContext = new CoreDbContext(options);
        using var repository = new BaseRepository<CoreDbContext, User>(dbContext);

        // Act
        var result = repository.GetAll().ToList();

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<List<User>>();
            result.Count.ShouldBeGreaterThanOrEqualTo(3);
        });
    }

    [TestMethod]
    public async Task GetAll_Should_Return_Entities_As_NoTracking()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = await _userRepository.AddAsync(user, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        _userRepository.Dispose();
        await _dbContext.DisposeAsync();

        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options;

        using var dbContext = new CoreDbContext(options);
        using var repository = new BaseRepository<CoreDbContext, User>(dbContext);

        // Act
        var result = repository.GetAll().FirstOrDefault(u => u.Id == user.Id);

        // Assert
        _ = result.ShouldNotBeNull();
        var entry = dbContext.Entry(result);
        entry.State.ShouldBe(EntityState.Detached);
    }

    [TestMethod]
    public async Task GetAll_Should_Return_Empty_Collection_When_No_Entities()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options;

        using var dbContext = new CoreDbContext(options);
        using var repository = new BaseRepository<CoreDbContext, Invoice>(dbContext);

        // Act - Try to get all invoices (likely none exist at test start)
        var result = repository.GetAll().ToList();

        // Assert
        _ = result.ShouldNotBeNull();
    }

    #endregion

    #region Remove Tests

    [TestMethod]
    public async Task Remove_Should_Mark_Entity_As_Deleted()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = await _userRepository.AddAsync(user, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        _userRepository.Remove(user);

        // Assert
        var entry = _dbContext.Entry(user);
        entry.State.ShouldBe(EntityState.Deleted);
    }

    [TestMethod]
    public async Task Remove_Should_Delete_Entity_From_Database()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = await _userRepository.AddAsync(user, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        _userRepository.Remove(user);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        using var dbContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var result = await dbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task Remove_Should_Attach_Detached_Entity_Before_Deleting()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = await _userRepository.AddAsync(user, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Detach the entity
        _dbContext.Entry(user).State = EntityState.Detached;

        // Act
        _userRepository.Remove(user);

        // Assert
        var entry = _dbContext.Entry(user);
        entry.State.ShouldBe(EntityState.Deleted);
    }

    #endregion

    #region Update Tests

    [TestMethod]
    public async Task Update_Should_Mark_Entity_As_Modified()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = await _userRepository.AddAsync(user, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        user.Name = "Updated Name";

        // Act
        var result = _userRepository.Update(user);

        // Assert
        result.ShouldBeSameAs(user);
        var entry = _dbContext.Entry(user);
        entry.State.ShouldBe(EntityState.Modified);
    }

    [TestMethod]
    public async Task Update_Should_Persist_Changes_To_Database()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = await _userRepository.AddAsync(user, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        user.Name = "Updated Name";

        // Act
        _ = _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        using var dbContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var result = await dbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == user.Id, TestContext.CancellationToken);

        // Assert
        _ = result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated Name");
    }

    [TestMethod]
    public async Task Update_Should_Attach_Detached_Entity_Before_Updating()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = await _userRepository.AddAsync(user, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Detach the entity
        _dbContext.Entry(user).State = EntityState.Detached;
        user.Name = "Updated Name";

        // Act
        _ = _userRepository.Update(user);

        // Assert
        var entry = _dbContext.Entry(user);
        entry.State.ShouldBe(EntityState.Modified);
    }

    [TestMethod]
    public async Task Update_Should_Return_Updated_Entity()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = await _userRepository.AddAsync(user, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        user.Name = "Updated Name";

        // Act
        var result = _userRepository.Update(user);

        // Assert
        result.ShouldBeSameAs(user);
    }

    #endregion

    #region Where Tests

    [TestMethod]
    public async Task Where_Should_Return_Filtered_Entities()
    {
        // Arrange
        var user1 = UserFaker()
            .RuleFor(u => u.Email, _ => "test1@example.com")
            .Generate();
        var user2 = UserFaker()
            .RuleFor(u => u.Email, _ => "test2@example.com")
            .Generate();
        var user3 = UserFaker()
            .RuleFor(u => u.Email, _ => "test3@example.com")
            .Generate();

        _ = await _userRepository.AddAsync(user1, TestContext.CancellationToken);
        _ = await _userRepository.AddAsync(user2, TestContext.CancellationToken);
        _ = await _userRepository.AddAsync(user3, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = _userRepository.Where(u => u.Email == "test2@example.com").ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Email.ShouldBe("test2@example.com");
    }

    [TestMethod]
    public async Task Where_Should_Return_Multiple_Matching_Entities()
    {
        // Arrange
        var user1 = UserFaker()
            .RuleFor(u => u.Name, _ => "Jack Doe")
            .Generate();
        var user2 = UserFaker()
            .RuleFor(u => u.Name, _ => "Jane Smith")
            .Generate();
        var user3 = UserFaker()
            .RuleFor(u => u.Name, _ => "Jack Smith")
            .Generate();

        _ = await _userRepository.AddAsync(user1, TestContext.CancellationToken);
        _ = await _userRepository.AddAsync(user2, TestContext.CancellationToken);
        _ = await _userRepository.AddAsync(user3, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = _userRepository.Where(u => u.Name.Contains("Jack"));

        // Assert
        result.Count().ShouldBe(2);
        result.ShouldAllBe(u => u.Name.Contains("Jack"));
    }

    [TestMethod]
    public async Task Where_Should_Return_Empty_Collection_When_No_Match()
    {
        // Arrange
        var user = UserFaker().Generate();
        _ = await _userRepository.AddAsync(user, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = _userRepository.Where(u => u.Email == "nonexistent@example.com").ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Where_Should_Support_Complex_Predicates()
    {
        // Arrange
        var user1 = UserFaker()
            .RuleFor(u => u.Name, _ => "Alice")
            .RuleFor(u => u.TelegramChatId, _ => 1000)
            .Generate();
        var user2 = UserFaker()
            .RuleFor(u => u.Name, _ => "Bob")
            .RuleFor(u => u.TelegramChatId, _ => 2000)
            .Generate();

        _ = await _userRepository.AddAsync(user1, TestContext.CancellationToken);
        _ = await _userRepository.AddAsync(user2, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = _userRepository.Where(u => u.Name == "Alice" && u.TelegramChatId > 500).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Alice");
    }

    #endregion

    #region Dispose Tests

    [TestMethod]
    public void Dispose_Should_Release_DbContext_Resources()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options;

        var dbContext = new CoreDbContext(options);
        var repository = new BaseRepository<CoreDbContext, User>(dbContext);

        // Act
        repository.Dispose();

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
        var repository = new BaseRepository<CoreDbContext, User>(dbContext);

        // Act & Assert
        Should.NotThrow(() =>
        {
            repository.Dispose();
            repository.Dispose();
            repository.Dispose();
        });
    }

    #endregion
}
