using InvoiceReminder.Data.Persistence.EntitiesConfig;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Shouldly;

namespace InvoiceReminder.UnitTests.Infrastructure.Data.EntitiesConfig;

[TestClass]
public sealed class UserConfigTests
{
    [TestMethod]
    public void UserConfig_ShouldNotThrowErrorWhenInstantiated()
    {
        // Arrange && Act
        Action action = () => _ = new UserConfig();

        // Assert
        action.ShouldNotThrow();
    }

    [TestMethod]
    public void UserConfig_ShouldConfigureEntityCorrectly()
    {
        // Arrange
        var builder = new ModelBuilder(new ConventionSet());
        var config = new UserConfig();

        // Act
        config.Configure(builder.Entity<User>());

        // Assert
        var entityType = builder.Model.FindEntityType(typeof(User));
        _ = entityType.ShouldNotBeNull();

        // Verifica tabela
        entityType.GetTableName().ShouldBe("user");

        // Verifica chave primária
        var primaryKey = entityType.FindPrimaryKey();
        _ = primaryKey.ShouldNotBeNull();
        primaryKey.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(User.Id));

        // Verifica propriedade Id
        var idProperty = entityType.FindProperty(nameof(User.Id));
        _ = idProperty.ShouldNotBeNull();
        idProperty.GetColumnName().ShouldBe("id");
        idProperty.GetColumnType().ShouldBe("uuid");
        (!idProperty.IsNullable).ShouldBeTrue();
        idProperty.ValueGenerated.ShouldBe(ValueGenerated.OnAdd);

        // Verifica propriedade TelegramChatId
        var telegramChatIdProperty = entityType.FindProperty(nameof(User.TelegramChatId));
        _ = telegramChatIdProperty.ShouldNotBeNull();
        telegramChatIdProperty.GetColumnName().ShouldBe("telegram_chat_id");
        telegramChatIdProperty.GetColumnType().ShouldBe("bigint");
        (!telegramChatIdProperty.IsNullable).ShouldBeTrue();

        // Verifica propriedade Name
        var nameProperty = entityType.FindProperty(nameof(User.Name));
        _ = nameProperty.ShouldNotBeNull();
        nameProperty.GetColumnName().ShouldBe("name");
        (!nameProperty.IsNullable).ShouldBeTrue();

        // Verifica propriedade Email
        var emailProperty = entityType.FindProperty(nameof(User.Email));
        _ = emailProperty.ShouldNotBeNull();
        emailProperty.GetColumnName().ShouldBe("email");
        (!emailProperty.IsNullable).ShouldBeTrue();

        // Verifica propriedade Password
        var passwordProperty = entityType.FindProperty(nameof(User.Password));
        _ = passwordProperty.ShouldNotBeNull();
        passwordProperty.GetColumnName().ShouldBe("password");
        (!passwordProperty.IsNullable).ShouldBeTrue();

        // Verifica propriedade CreatedAt (herdada de EntityDefaults)
        var createdAtProperty = entityType.FindProperty(nameof(User.CreatedAt));
        _ = createdAtProperty.ShouldNotBeNull();
        createdAtProperty.GetColumnName().ShouldBe("created_at");
        createdAtProperty.GetColumnType().ShouldBe("timestamp with time zone");
        (!createdAtProperty.IsNullable).ShouldBeTrue();

        // Verifica propriedade UpdatedAt (herdada de EntityDefaults)
        var updatedAtProperty = entityType.FindProperty(nameof(User.UpdatedAt));
        _ = updatedAtProperty.ShouldNotBeNull();
        updatedAtProperty.GetColumnName().ShouldBe("updated_at");
        updatedAtProperty.GetColumnType().ShouldBe("timestamp with time zone");
        (!updatedAtProperty.IsNullable).ShouldBeTrue();
    }
}
