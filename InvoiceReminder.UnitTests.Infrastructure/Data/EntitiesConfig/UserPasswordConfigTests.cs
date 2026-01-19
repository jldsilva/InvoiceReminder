using InvoiceReminder.Data.Persistence.EntitiesConfig;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Shouldly;

namespace InvoiceReminder.UnitTests.Infrastructure.Data.EntitiesConfig;

[TestClass]
public sealed class UserPasswordConfigTests
{
    [TestMethod]
    public void UserPasswordConfig_ShouldConfigureEntityCorrectly()
    {
        // Arrange
        var builder = new ModelBuilder(new ConventionSet());
        var config = new UserPasswordConfig();

        // Act
        config.Configure(builder.Entity<UserPassword>());

        // Assert
        var entityType = builder.Model.FindEntityType(typeof(UserPassword));
        _ = entityType.ShouldNotBeNull();

        // Verifica tabela
        entityType.GetTableName().ShouldBe("user_password");

        // Verifica chave primária
        var primaryKey = entityType.FindPrimaryKey();
        _ = primaryKey.ShouldNotBeNull();
        primaryKey.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(UserPassword.Id));

        // Verifica propriedade Id
        var idProperty = entityType.FindProperty(nameof(UserPassword.Id));
        _ = idProperty.ShouldNotBeNull();
        idProperty.GetColumnName().ShouldBe("id");
        idProperty.GetColumnType().ShouldBe("uuid");
        idProperty.IsNullable.ShouldBeFalse();
        idProperty.ValueGenerated.ShouldBe(ValueGenerated.OnAdd);

        // Verifica propriedade UserId
        var userIdProperty = entityType.FindProperty(nameof(UserPassword.UserId));
        _ = userIdProperty.ShouldNotBeNull();
        userIdProperty.GetColumnName().ShouldBe("user_id");
        userIdProperty.GetColumnType().ShouldBe("uuid");
        userIdProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade PasswordHash
        var passwordHashProperty = entityType.FindProperty(nameof(UserPassword.PasswordHash));
        _ = passwordHashProperty.ShouldNotBeNull();
        passwordHashProperty.GetColumnName().ShouldBe("password_hash");
        passwordHashProperty.GetMaxLength().ShouldBe(512);
        passwordHashProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade PasswordSalt
        var passwordSaltProperty = entityType.FindProperty(nameof(UserPassword.PasswordSalt));
        _ = passwordSaltProperty.ShouldNotBeNull();
        passwordSaltProperty.GetColumnName().ShouldBe("password_salt");
        passwordSaltProperty.GetMaxLength().ShouldBe(256);
        passwordSaltProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade CreatedAt (herdada de EntityDefaults)
        var createdAtProperty = entityType.FindProperty(nameof(UserPassword.CreatedAt));
        _ = createdAtProperty.ShouldNotBeNull();
        createdAtProperty.GetColumnName().ShouldBe("created_at");
        createdAtProperty.GetColumnType().ShouldBe("timestamp with time zone");
        createdAtProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade UpdatedAt (herdada de EntityDefaults)
        var updatedAtProperty = entityType.FindProperty(nameof(UserPassword.UpdatedAt));
        _ = updatedAtProperty.ShouldNotBeNull();
        updatedAtProperty.GetColumnName().ShouldBe("updated_at");
        updatedAtProperty.GetColumnType().ShouldBe("timestamp with time zone");
        updatedAtProperty.IsNullable.ShouldBeFalse();
    }
}
