using InvoiceReminder.Data.Repository;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using System.Linq.Expressions;

namespace InvoiceReminder.Infrastructure.UnitTests.Data.Repository;

[TestClass]
public sealed class BaseRepositoryTests
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TestDbContext> _contextOptions;

    public TestContext TestContext { get; set; }

    public BaseRepositoryTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    [TestInitialize]
    public async Task Setup()
    {
        using var context = new TestDbContext(_contextOptions);
        _ = await context.Database.EnsureCreatedAsync(TestContext.CancellationToken);
    }

    [TestCleanup]
    public void TearDown()
    {
        _connection.Dispose();
    }

    private TestDbContext CreateContext()
    {
        return new(_contextOptions);
    }

    [TestMethod]
    public async Task AddAsync_Should_AddEntityToDatabase()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        using var context = CreateContext();
        var repository = new BaseRepository<TestDbContext, TestEntity>(context);

        // Act
        var addedEntity = await repository.AddAsync(entity, TestContext.CancellationToken);
        _ = await context.SaveChangesAsync(TestContext.CancellationToken);

        // Assert
        addedEntity.ShouldBeSameAs(entity);
        context.TestEntities.ShouldContain(entity);
    }

    [TestMethod]
    public async Task BulkInsertAsync_Should_AddMultipleEntitiesToDatabaseWithTimestamps()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Test1" },
            new() { Id = Guid.NewGuid(), Name = "Test2" }
        };

        using var context = CreateContext();
        var repository = new BaseRepository<TestDbContext, TestEntity>(context);

        // Act
        var count = await repository.BulkInsertAsync(entities, TestContext.CancellationToken);
        _ = await context.SaveChangesAsync(TestContext.CancellationToken);
        var total = await context.TestEntities.CountAsync(TestContext.CancellationToken);

        // Assert
        count.ShouldBe(entities.Count);

        context.ShouldSatisfyAllConditions(() =>
        {
            total.ShouldBe(entities.Count);
            context.TestEntities.ShouldAllBe(e => e.CreatedAt.HasValue && e.UpdatedAt.HasValue);
        });

    }

    [TestMethod]
    public async Task Remove_Should_RemoveExistingEntityFromDatabase()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        using var context = CreateContext();
        var repository = new BaseRepository<TestDbContext, TestEntity>(context);
        _ = await repository.AddAsync(entity, TestContext.CancellationToken);
        _ = await context.SaveChangesAsync(TestContext.CancellationToken);


        // Act
        repository.Remove(entity);
        _ = await context.SaveChangesAsync(TestContext.CancellationToken);

        // Assert
        context.TestEntities.ShouldNotContain(entity);
    }

    [TestMethod]
    public async Task Remove_Should_AttachAndRemoveDetachedEntity()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        using var context = CreateContext();
        var repository = new BaseRepository<TestDbContext, TestEntity>(context);

        _ = await repository.AddAsync(entity, TestContext.CancellationToken);
        _ = await context.SaveChangesAsync(TestContext.CancellationToken);

        _ = context.Attach(entity);
        context.Entry(entity).State = EntityState.Detached;

        // Act
        repository.Remove(entity);
        _ = await context.SaveChangesAsync(TestContext.CancellationToken);

        // Assert
        context.TestEntities.ShouldNotContain(entity);
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_ReturnEntityById()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        using var context = CreateContext();
        var repository = new BaseRepository<TestDbContext, TestEntity>(context);

        _ = await repository.AddAsync(entity, TestContext.CancellationToken);
        _ = await context.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var retrievedEntity = await repository.GetByIdAsync(entity.Id, TestContext.CancellationToken);

        // Assert
        retrievedEntity.ShouldSatisfyAllConditions(() =>
        {
            _ = retrievedEntity.ShouldNotBeNull();
            retrievedEntity.Id.ShouldBe(entity.Id);
            retrievedEntity.Name.ShouldBe(entity.Name);
        });
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_ReturnNullWhenEntityNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        using var context = CreateContext();
        var repository = new BaseRepository<TestDbContext, TestEntity>(context);

        // Act
        var retrievedEntity = await repository.GetByIdAsync(nonExistingId, TestContext.CancellationToken);

        // Assert
        retrievedEntity.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetAll_Should_ReturnAllEntities()
    {
        // Arrange
        var entities = new List<TestEntity>
    {
        new() { Id = Guid.NewGuid(), Name = "Test1" },
        new() { Id = Guid.NewGuid(), Name = "Test2" }
    };

        using var context = CreateContext();
        var repository = new BaseRepository<TestDbContext, TestEntity>(context);

        _ = await repository.BulkInsertAsync(entities, TestContext.CancellationToken);
        _ = await context.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var allEntities = repository.GetAll().ToList();

        // Assert
        allEntities.ShouldSatisfyAllConditions(() =>
        {
            _ = allEntities.ShouldNotBeNull();
            allEntities.Count.ShouldBe(entities.Count);
            allEntities.ShouldBeEquivalentTo(entities);
        });
    }

    [TestMethod]
    public async Task Update_Should_UpdateExistingEntity()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Original Name" };
        using var context = CreateContext();
        var repository = new BaseRepository<TestDbContext, TestEntity>(context);

        _ = await repository.AddAsync(entity, TestContext.CancellationToken);
        _ = await context.SaveChangesAsync(TestContext.CancellationToken);

        entity.Name = "Updated Name";

        // Act
        var updatedEntity = repository.Update(entity);
        _ = await context.SaveChangesAsync(TestContext.CancellationToken);
        var retrievedEntity = await repository.GetByIdAsync(entity.Id, TestContext.CancellationToken);

        // Assert
        updatedEntity.ShouldBeSameAs(entity);

        retrievedEntity.ShouldSatisfyAllConditions(() =>
        {
            _ = retrievedEntity.ShouldNotBeNull();
            retrievedEntity.Name.ShouldBe("Updated Name");
        });
    }

    [TestMethod]
    public async Task Update_Should_AttachAndUpdateDetachedEntity()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Original Name" };
        using var context = CreateContext();
        var repository = new BaseRepository<TestDbContext, TestEntity>(context);

        _ = await repository.AddAsync(entity, TestContext.CancellationToken);
        _ = await context.SaveChangesAsync(TestContext.CancellationToken);

        _ = context.Attach(entity);
        context.Entry(entity).State = EntityState.Detached;

        entity.Name = "Updated Name";

        // Act
        var updatedEntity = repository.Update(entity);
        _ = await context.SaveChangesAsync(TestContext.CancellationToken);
        var retrievedEntity = await repository.GetByIdAsync(entity.Id, TestContext.CancellationToken);

        // Assert
        updatedEntity.ShouldBeSameAs(entity);

        retrievedEntity.ShouldSatisfyAllConditions(() =>
        {
            _ = retrievedEntity.ShouldNotBeNull();
            retrievedEntity.Name.ShouldBe("Updated Name");
        });
    }

    [TestMethod]
    public async Task Where_Should_ReturnEntitiesMatchingPredicate()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Test1" },
            new() { Id = Guid.NewGuid(), Name = "AnotherTest" },
            new() { Id = Guid.NewGuid(), Name = "Test2" }
        };

        using var context = CreateContext();
        context.TestEntities.AddRange(entities);
        _ = await context.SaveChangesAsync(TestContext.CancellationToken);
        var repository = new BaseRepository<TestDbContext, TestEntity>(context);

        // Act
        Expression<Func<TestEntity, bool>> predicate = e => e.Name.StartsWith("Test");
        var filteredEntities = repository.Where(predicate).ToList();

        // Assert
        filteredEntities.ShouldSatisfyAllConditions(() =>
        {
            filteredEntities.Count.ShouldBe(2);
            filteredEntities.ShouldContain(entities[0]);
            filteredEntities.ShouldNotContain(entities[1]);
            filteredEntities.ShouldContain(entities[2]);
        });
    }

    [TestMethod]
    public void Dispose_Should_DisposeDbContext()
    {
        // Arrange
        var mockConnection = Substitute.For<SqliteConnection>();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(mockConnection)
            .Options;
        var mockDbContext = Substitute.For<TestDbContext>(options);
        var repository = new BaseRepository<TestDbContext, TestEntity>(mockDbContext);

        // Act
        repository.Dispose();

        // Assert
        mockDbContext.Received(1).Dispose();
    }

    [TestMethod]
    public void Dispose_CalledMultipleTimes_Should_DisposeDbContextOnlyOnce()
    {
        // Arrange
        var mockConnection = Substitute.For<SqliteConnection>();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(mockConnection)
            .Options;
        var mockDbContext = Substitute.For<TestDbContext>(options);
        var repository = new BaseRepository<TestDbContext, TestEntity>(mockDbContext);

        // Act
        repository.Dispose();
        repository.Dispose();

        // Assert
        mockDbContext.Received(1).Dispose();
    }
}

public class TestDbContext : DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; }

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
        base.OnModelCreating(modelBuilder);
    }
}

public class TestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
