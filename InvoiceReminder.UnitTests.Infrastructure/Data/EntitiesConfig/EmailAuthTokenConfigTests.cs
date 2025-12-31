using InvoiceReminder.Data.Persistence.EntitiesConfig;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Shouldly;

namespace InvoiceReminder.UnitTests.Infrastructure.Data.EntitiesConfig;

[TestClass]
public sealed class EmailAuthTokenConfigTests
{
    [TestMethod]
    public void EmailAuthTokenConfig_ShouldConfigureEntityCorrectly()
    {
        // Arrange
        var builder = new ModelBuilder(new ConventionSet());
        var config = new EmailAuthTokenConfig();

        // Act
        config.Configure(builder.Entity<EmailAuthToken>());

        // Assert
        var entityType = builder.Model.FindEntityType(typeof(EmailAuthToken));
        _ = entityType.ShouldNotBeNull();

        // Verifica tabela
        entityType.GetTableName().ShouldBe("email_auth_token");

        // Verifica chave primária
        var primaryKey = entityType.FindPrimaryKey();
        _ = primaryKey.ShouldNotBeNull();
        primaryKey.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(EmailAuthToken.Id));

        // Verifica propriedade Id
        var idProperty = entityType.FindProperty(nameof(EmailAuthToken.Id));
        _ = idProperty.ShouldNotBeNull();
        idProperty.GetColumnName().ShouldBe("id");
        idProperty.GetColumnType().ShouldBe("uuid");
        (!idProperty.IsNullable).ShouldBeTrue();
        idProperty.ValueGenerated.ShouldBe(ValueGenerated.OnAdd);

        // Verifica propriedade UserId
        var userIdProperty = entityType.FindProperty(nameof(EmailAuthToken.UserId));
        _ = userIdProperty.ShouldNotBeNull();
        userIdProperty.GetColumnName().ShouldBe("user_id");
        userIdProperty.GetColumnType().ShouldBe("uuid");
        userIdProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade AccessToken
        var accessTokenProperty = entityType.FindProperty(nameof(EmailAuthToken.AccessToken));
        _ = accessTokenProperty.ShouldNotBeNull();
        accessTokenProperty.GetColumnName().ShouldBe("access_token");
        accessTokenProperty.GetMaxLength().ShouldBe(512);
        accessTokenProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade RefreshToken
        var refreshTokenProperty = entityType.FindProperty(nameof(EmailAuthToken.RefreshToken));
        _ = refreshTokenProperty.ShouldNotBeNull();
        refreshTokenProperty.GetColumnName().ShouldBe("refresh_token");
        refreshTokenProperty.GetMaxLength().ShouldBe(512);
        refreshTokenProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade NonceValue
        var nonceValueProperty = entityType.FindProperty(nameof(EmailAuthToken.NonceValue));
        _ = nonceValueProperty.ShouldNotBeNull();
        nonceValueProperty.GetColumnName().ShouldBe("nonce_value");
        nonceValueProperty.GetMaxLength().ShouldBe(64);
        nonceValueProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade TokenProvider
        var tokenProviderProperty = entityType.FindProperty(nameof(EmailAuthToken.TokenProvider));
        _ = tokenProviderProperty.ShouldNotBeNull();
        tokenProviderProperty.GetColumnName().ShouldBe("token_provider");
        tokenProviderProperty.GetMaxLength().ShouldBe(25);
        tokenProviderProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade AccessTokenExpiry
        var accessTokenExpiryProperty = entityType.FindProperty(nameof(EmailAuthToken.AccessTokenExpiry));
        _ = accessTokenExpiryProperty.ShouldNotBeNull();
        accessTokenExpiryProperty.GetColumnName().ShouldBe("access_token_expiry");
        accessTokenExpiryProperty.GetColumnType().ShouldBe("timestamp with time zone");
        accessTokenExpiryProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade CreatedAt (herdada de EntityDefaults)
        var createdAtProperty = entityType.FindProperty(nameof(EmailAuthToken.CreatedAt));
        _ = createdAtProperty.ShouldNotBeNull();
        createdAtProperty.GetColumnName().ShouldBe("created_at");
        createdAtProperty.GetColumnType().ShouldBe("timestamp with time zone");
        createdAtProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade UpdatedAt (herdada de EntityDefaults)
        var updatedAtProperty = entityType.FindProperty(nameof(EmailAuthToken.UpdatedAt));
        _ = updatedAtProperty.ShouldNotBeNull();
        updatedAtProperty.GetColumnName().ShouldBe("updated_at");
        updatedAtProperty.GetColumnType().ShouldBe("timestamp with time zone");
        updatedAtProperty.IsNullable.ShouldBeFalse();
    }
}
