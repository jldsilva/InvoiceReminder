using InvoiceReminder.Domain.Extensions;
using Shouldly;

namespace InvoiceReminder.DomainEntities.UnitTests.Extensions;

[TestClass]
public sealed class EntityExtensionsTests
{
    private sealed class TestEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

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
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "Test Entity" };
        var collection = new List<TestEntity>();

        // Act
        entity.AddIfNotExists(collection);

        // Assert
        collection.ShouldSatisfyAllConditions(() =>
        {
            _ = collection.ShouldHaveSingleItem();
            collection[0].ShouldBeEquivalentTo(entity);
            collection[0].Id.ShouldBe(entityId);
        });
    }

    [TestMethod]
    public void AddIfNotExists_WhenEntityAlreadyExists_ShouldNotAddDuplicate()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var existingEntity = new TestEntity { Id = entityId, Name = "Existing Entity" };
        var newEntity = new TestEntity { Id = entityId, Name = "New Entity" };
        var collection = new List<TestEntity> { existingEntity };

        // Act
        newEntity.AddIfNotExists(collection);

        // Assert
        collection.ShouldSatisfyAllConditions(() =>
        {
            _ = collection.ShouldHaveSingleItem();
            collection[0].ShouldBeEquivalentTo(existingEntity);
            collection[0].Name.ShouldBe("Existing Entity");
        });
    }

    [TestMethod]
    public void AddIfNotExists_WhenEntityDoesNotExist_ShouldAddToNonEmptyCollection()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        var existingEntity = new TestEntity { Id = existingId, Name = "Existing Entity" };
        var newEntity = new TestEntity { Id = newId, Name = "New Entity" };
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
        var targetId = Guid.NewGuid();
        var entity1 = new TestEntity { Id = Guid.NewGuid(), Name = "Entity 1" };
        var entity2 = new TestEntity { Id = targetId, Name = "Entity 2" };
        var entity3 = new TestEntity { Id = Guid.NewGuid(), Name = "Entity 3" };
        var duplicateEntity = new TestEntity { Id = targetId, Name = "Different Name" };
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
        var collection = new List<TestEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Entity 1" },
            new() { Id = Guid.NewGuid(), Name = "Entity 2" }
        };
        var initialCount = collection.Count;
        var newEntity = new TestEntity { Id = Guid.NewGuid(), Name = "Entity 3" };

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
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "Test Entity" };
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
        var entity = new TestEntity { Id = Guid.Empty, Name = "Entity with Empty ID" };
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
        var existingEntity = new TestEntity { Id = Guid.Empty, Name = "Existing Entity" };
        var newEntity = new TestEntity { Id = Guid.Empty, Name = "New Entity" };
        var collection = new List<TestEntity> { existingEntity };

        // Act
        newEntity.AddIfNotExists(collection);

        // Assert
        collection.ShouldSatisfyAllConditions(() =>
        {
            _ = collection.ShouldHaveSingleItem();
            collection[0].Name.ShouldBe("Existing Entity");
        });
    }

    [TestMethod]
    public void AddIfNotExists_WithHashSetCollection_ShouldWorkCorrectly()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "Test Entity" };
        var collection = new HashSet<TestEntity>([new TestEntity { Id = Guid.NewGuid(), Name = "Existing" }]);

        // Act
        entity.AddIfNotExists(collection);

        // Assert
        collection.ShouldSatisfyAllConditions(() =>
        {
            collection.Count.ShouldBe(2);
            collection.ShouldContain(entity);
        });
    }

    [TestMethod]
    public void AddIfNotExists_WhenPreservingExistingEntityProperties_ShouldNotReplaceExistingEntity()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var existingEntity = new TestEntity { Id = entityId, Name = "Original Name" };
        var newEntity = new TestEntity { Id = entityId, Name = "Updated Name" };
        var collection = new List<TestEntity> { existingEntity };

        // Act
        newEntity.AddIfNotExists(collection);

        // Assert
        collection.ShouldSatisfyAllConditions(() =>
        {
            _ = collection.ShouldHaveSingleItem();
            collection[0].Name.ShouldBe("Original Name");
            collection[0].ShouldBe(existingEntity);
        });
    }
}

