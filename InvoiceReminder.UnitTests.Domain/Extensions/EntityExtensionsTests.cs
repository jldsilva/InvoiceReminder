using Bogus;
using InvoiceReminder.Domain.Extensions;
using Shouldly;

namespace InvoiceReminder.UnitTests.Domain.Extensions;

[TestClass]
public sealed class EntityExtensionsTests
{
    private sealed class TestEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    private readonly Faker<TestEntity> _entityFaker = new Faker<TestEntity>()
        .RuleFor(e => e.Id, faker => faker.Random.Guid())
        .RuleFor(e => e.Name, faker => faker.Person.FullName);

    [TestMethod]
    public void AddIfNotExists_WhenEntityIsNull_ShouldNotAddToCollection()
    {
        // Arrange
        TestEntity entity = null;
        var collection = new List<TestEntity>();

        // Act
        entity.AddIfNotExists(collection);

        // Assert
        collection.ShouldBeEmpty();
    }

    [TestMethod]
    public void AddIfNotExists_WhenCollectionIsEmpty_ShouldAddEntity()
    {
        // Arrange
        var entity = _entityFaker.Generate();
        var collection = new List<TestEntity>();

        // Act
        entity.AddIfNotExists(collection);

        // Assert
        collection.ShouldSatisfyAllConditions(() =>
        {
            _ = collection.ShouldHaveSingleItem();
            collection[0].ShouldBeEquivalentTo(entity);
            collection[0].Id.ShouldBe(entity.Id);
        });
    }

    [TestMethod]
    public void AddIfNotExists_WhenEntityAlreadyExists_ShouldNotAddDuplicate()
    {
        // Arrange
        var faker = new Faker();
        var entityId = faker.Random.Guid();
        var existingEntity = new TestEntity { Id = entityId, Name = faker.Person.FullName };
        var newEntity = new TestEntity { Id = entityId, Name = faker.Person.FullName };
        var collection = new List<TestEntity> { existingEntity };

        // Act
        newEntity.AddIfNotExists(collection);

        // Assert
        collection.ShouldSatisfyAllConditions(() =>
        {
            _ = collection.ShouldHaveSingleItem();
            collection[0].ShouldBeEquivalentTo(existingEntity);
            collection[0].Id.ShouldBe(entityId);
        });
    }

    [TestMethod]
    public void AddIfNotExists_WhenEntityDoesNotExist_ShouldAddToNonEmptyCollection()
    {
        // Arrange
        var existingEntity = _entityFaker.Generate();
        var newEntity = _entityFaker.Generate();
        var collection = new List<TestEntity> { existingEntity };

        // Act
        newEntity.AddIfNotExists(collection);

        // Assert
        collection.ShouldSatisfyAllConditions(() =>
        {
            collection.Count.ShouldBe(2);
            collection.ShouldContain(existingEntity);
            collection.ShouldContain(newEntity);
        });
    }

    [TestMethod]
    public void AddIfNotExists_WhenMultipleEntitiesExist_ShouldFindExistingByIdOnly()
    {
        // Arrange
        var faker = new Faker();
        var targetId = faker.Random.Guid();
        var entity1 = _entityFaker.Generate();
        var entity2 = new TestEntity { Id = targetId, Name = faker.Person.FullName };
        var entity3 = _entityFaker.Generate();
        var duplicateEntity = new TestEntity { Id = targetId, Name = faker.Person.FullName };
        var collection = new List<TestEntity> { entity1, entity2, entity3 };

        // Act
        duplicateEntity.AddIfNotExists(collection);

        // Assert
        collection.ShouldSatisfyAllConditions(() =>
        {
            collection.Count.ShouldBe(3);
            collection.Count(e => e.Id == targetId).ShouldBe(1);
        });
    }

    [TestMethod]
    public void AddIfNotExists_WhenEntityWithNewIdAdded_ShouldIncreaseCollectionCount()
    {
        // Arrange
        var collection = new List<TestEntity> { _entityFaker.Generate(), _entityFaker.Generate() };
        var initialCount = collection.Count;
        var newEntity = _entityFaker.Generate();

        // Act
        newEntity.AddIfNotExists(collection);

        // Assert
        collection.ShouldSatisfyAllConditions(() =>
        {
            collection.Count.ShouldBe(initialCount + 1);
            collection[^1].ShouldBe(newEntity);
        });
    }

    [TestMethod]
    public void AddIfNotExists_WhenMultipleCallsWithSameEntity_ShouldOnlyAddOnce()
    {
        // Arrange
        var entity = _entityFaker.Generate();
        var collection = new List<TestEntity>();

        // Act
        entity.AddIfNotExists(collection);
        entity.AddIfNotExists(collection);
        entity.AddIfNotExists(collection);

        // Assert
        collection.ShouldSatisfyAllConditions(() =>
        {
            _ = collection.ShouldHaveSingleItem();
            collection[0].ShouldBeEquivalentTo(entity);
        });
    }

    [TestMethod]
    public void AddIfNotExists_WhenEntityIdIsEmptyGuid_ShouldAddEntity()
    {
        // Arrange
        var faker = new Faker();
        var entity = new TestEntity { Id = Guid.Empty, Name = faker.Person.FullName };
        var collection = new List<TestEntity>();

        // Act
        entity.AddIfNotExists(collection);

        // Assert
        collection.ShouldSatisfyAllConditions(() =>
        {
            _ = collection.ShouldHaveSingleItem();
            collection[0].Id.ShouldBe(Guid.Empty);
        });
    }

    [TestMethod]
    public void AddIfNotExists_WhenEntityWithEmptyIdAlreadyExists_ShouldNotAddDuplicate()
    {
        // Arrange
        var faker = new Faker();
        var existingEntity = new TestEntity { Id = Guid.Empty, Name = faker.Person.FullName };
        var newEntity = new TestEntity { Id = Guid.Empty, Name = faker.Person.FullName };
        var collection = new List<TestEntity> { existingEntity };

        // Act
        newEntity.AddIfNotExists(collection);

        // Assert
        collection.ShouldSatisfyAllConditions(() =>
        {
            _ = collection.ShouldHaveSingleItem();
            collection[0].Id.ShouldBe(Guid.Empty);
        });
    }

    [TestMethod]
    public void AddIfNotExists_WithHashSetCollection_ShouldWorkCorrectly()
    {
        // Arrange
        var newEntity = _entityFaker.Generate();
        var existingEntity = _entityFaker.Generate();
        var collection = new HashSet<TestEntity> { existingEntity };

        // Act
        newEntity.AddIfNotExists(collection);

        // Assert
        collection.ShouldSatisfyAllConditions(() =>
        {
            collection.Count.ShouldBe(2);
            collection.ShouldContain(newEntity);
        });
    }

    [TestMethod]
    public void AddIfNotExists_WhenPreservingExistingEntityProperties_ShouldNotReplaceExistingEntity()
    {
        // Arrange
        var faker = new Faker();
        var entityId = faker.Random.Guid();
        var existingName = faker.Person.FullName;
        var existingEntity = new TestEntity { Id = entityId, Name = existingName };
        var newEntity = new TestEntity { Id = entityId, Name = faker.Person.FullName };
        var collection = new List<TestEntity> { existingEntity };

        // Act
        newEntity.AddIfNotExists(collection);

        // Assert
        collection.ShouldSatisfyAllConditions(() =>
        {
            _ = collection.ShouldHaveSingleItem();
            collection[0].Name.ShouldBe(existingName);
            collection[0].ShouldBe(existingEntity);
        });
    }
}
