using InvoiceReminder.Data.Persistence.EntitiesConfig;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Shouldly;

namespace InvoiceReminder.UnitTests.Infrastructure.Data.EntitiesConfig;

[TestClass]
public sealed class InvoiceConfigTests
{
    [TestMethod]
    public void InvoiceConfig_ShouldNotThrowErrorWhenInstantiated()
    {
        // Arrange && Act
        Action action = () => _ = new InvoiceConfig();

        // Assert
        action.ShouldNotThrow();
    }

    [TestMethod]
    public void InvoiceConfig_ShouldConfigureEntityCorrectly()
    {
        // Arrange
        var builder = new ModelBuilder(new ConventionSet());
        var config = new InvoiceConfig();

        // Act
        config.Configure(builder.Entity<Invoice>());

        // Assert
        var entityType = builder.Model.FindEntityType(typeof(Invoice));
        _ = entityType.ShouldNotBeNull();

        // Verifica tabela
        entityType.GetTableName().ShouldBe("invoice");

        // Verifica chave primária
        var primaryKey = entityType.FindPrimaryKey();
        _ = primaryKey.ShouldNotBeNull();
        primaryKey.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(Invoice.Id));

        // Verifica propriedade Id
        var idProperty = entityType.FindProperty(nameof(Invoice.Id));
        _ = idProperty.ShouldNotBeNull();
        idProperty.GetColumnName().ShouldBe("id");
        idProperty.GetColumnType().ShouldBe("uuid");
        (!idProperty.IsNullable).ShouldBeTrue();
        idProperty.ValueGenerated.ShouldBe(ValueGenerated.OnAdd);

        // Verifica propriedade UserId
        var userIdProperty = entityType.FindProperty(nameof(Invoice.UserId));
        _ = userIdProperty.ShouldNotBeNull();
        userIdProperty.GetColumnName().ShouldBe("user_id");
        userIdProperty.GetColumnType().ShouldBe("uuid");
        (!userIdProperty.IsNullable).ShouldBeTrue();

        // Verifica propriedade Bank
        var bankProperty = entityType.FindProperty(nameof(Invoice.Bank));
        _ = bankProperty.ShouldNotBeNull();
        bankProperty.GetColumnName().ShouldBe("bank");
        (!bankProperty.IsNullable).ShouldBeTrue();
        bankProperty.GetMaxLength().ShouldBe(255);

        // Verifica propriedade Beneficiary
        var beneficiaryProperty = entityType.FindProperty(nameof(Invoice.Beneficiary));
        _ = beneficiaryProperty.ShouldNotBeNull();
        beneficiaryProperty.GetColumnName().ShouldBe("beneficiary");
        (!beneficiaryProperty.IsNullable).ShouldBeTrue();
        beneficiaryProperty.GetMaxLength().ShouldBe(255);

        // Verifica propriedade Barcode
        var barcodeProperty = entityType.FindProperty(nameof(Invoice.Barcode));
        _ = barcodeProperty.ShouldNotBeNull();
        barcodeProperty.GetColumnName().ShouldBe("barcode");
        (!barcodeProperty.IsNullable).ShouldBeTrue();

        // Verifica propriedade Amount
        var amountProperty = entityType.FindProperty(nameof(Invoice.Amount));
        _ = amountProperty.ShouldNotBeNull();
        amountProperty.GetColumnName().ShouldBe("amount");
        (!amountProperty.IsNullable).ShouldBeTrue();

        // Verifica propriedade DueDate
        var dueDateProperty = entityType.FindProperty(nameof(Invoice.DueDate));
        _ = dueDateProperty.ShouldNotBeNull();
        dueDateProperty.GetColumnName().ShouldBe("due_date");
        dueDateProperty.GetColumnType().ShouldBe("timestamp with time zone");
        (!dueDateProperty.IsNullable).ShouldBeTrue();

        // Verifica propriedade CreatedAt (herdada de EntityDefaults)
        var createdAtProperty = entityType.FindProperty(nameof(Invoice.CreatedAt));
        _ = createdAtProperty.ShouldNotBeNull();
        createdAtProperty.GetColumnName().ShouldBe("created_at");
        createdAtProperty.GetColumnType().ShouldBe("timestamp with time zone");
        (!createdAtProperty.IsNullable).ShouldBeTrue();

        // Verifica propriedade UpdatedAt (herdada de EntityDefaults)
        var updatedAtProperty = entityType.FindProperty(nameof(Invoice.UpdatedAt));
        _ = updatedAtProperty.ShouldNotBeNull();
        updatedAtProperty.GetColumnName().ShouldBe("updated_at");
        updatedAtProperty.GetColumnType().ShouldBe("timestamp with time zone");
        (!updatedAtProperty.IsNullable).ShouldBeTrue();
    }
}
