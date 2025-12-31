using InvoiceReminder.Data.Persistence.EntitiesConfig;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Shouldly;

namespace InvoiceReminder.UnitTests.Infrastructure.Data.EntitiesConfig;

[TestClass]
public sealed class ScanEmailDefinitionConfigTests
{
    [TestMethod]
    public void ScanEmailDefinitionConfig_ShouldConfigureEntityCorrectly()
    {
        // Arrange
        var builder = new ModelBuilder(new ConventionSet());
        var config = new ScanEmailDefinitionConfig();

        // Act
        config.Configure(builder.Entity<ScanEmailDefinition>());

        // Assert
        var entityType = builder.Model.FindEntityType(typeof(ScanEmailDefinition));
        _ = entityType.ShouldNotBeNull();

        // Verifica tabela
        entityType.GetTableName().ShouldBe("scan_email_definition");

        // Verifica chave primária
        var primaryKey = entityType.FindPrimaryKey();
        _ = primaryKey.ShouldNotBeNull();
        primaryKey.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(ScanEmailDefinition.Id));

        // Verifica propriedade Id
        var idProperty = entityType.FindProperty(nameof(ScanEmailDefinition.Id));
        _ = idProperty.ShouldNotBeNull();
        idProperty.GetColumnName().ShouldBe("id");
        idProperty.GetColumnType().ShouldBe("uuid");
        idProperty.IsNullable.ShouldBeFalse();
        idProperty.ValueGenerated.ShouldBe(ValueGenerated.OnAdd);

        // Verifica propriedade UserId
        var userIdProperty = entityType.FindProperty(nameof(ScanEmailDefinition.UserId));
        _ = userIdProperty.ShouldNotBeNull();
        userIdProperty.GetColumnName().ShouldBe("user_id");
        userIdProperty.GetColumnType().ShouldBe("uuid");
        userIdProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade InvoiceType
        var invoiceTypeProperty = entityType.FindProperty(nameof(ScanEmailDefinition.InvoiceType));
        _ = invoiceTypeProperty.ShouldNotBeNull();
        invoiceTypeProperty.GetColumnName().ShouldBe("invoice_type");
        invoiceTypeProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade Beneficiary
        var beneficiaryProperty = entityType.FindProperty(nameof(ScanEmailDefinition.Beneficiary));
        _ = beneficiaryProperty.ShouldNotBeNull();
        beneficiaryProperty.GetColumnName().ShouldBe("beneficiary");
        beneficiaryProperty.GetMaxLength().ShouldBe(255);
        beneficiaryProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade Description
        var descriptionProperty = entityType.FindProperty(nameof(ScanEmailDefinition.Description));
        _ = descriptionProperty.ShouldNotBeNull();
        descriptionProperty.GetColumnName().ShouldBe("description");
        descriptionProperty.GetMaxLength().ShouldBe(255);
        descriptionProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade SenderEmailAddress
        var senderEmailAddressProperty = entityType.FindProperty(nameof(ScanEmailDefinition.SenderEmailAddress));
        _ = senderEmailAddressProperty.ShouldNotBeNull();
        senderEmailAddressProperty.GetColumnName().ShouldBe("sender_email_address");
        senderEmailAddressProperty.GetMaxLength().ShouldBe(255);
        senderEmailAddressProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade AttachmentFileName
        var attachmentFileNameProperty = entityType.FindProperty(nameof(ScanEmailDefinition.AttachmentFileName));
        _ = attachmentFileNameProperty.ShouldNotBeNull();
        attachmentFileNameProperty.GetColumnName().ShouldBe("attachment_filename");
        attachmentFileNameProperty.GetMaxLength().ShouldBe(255);
        attachmentFileNameProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade CreatedAt (herdada de EntityDefaults)
        var createdAtProperty = entityType.FindProperty(nameof(ScanEmailDefinition.CreatedAt));
        _ = createdAtProperty.ShouldNotBeNull();
        createdAtProperty.GetColumnName().ShouldBe("created_at");
        createdAtProperty.GetColumnType().ShouldBe("timestamp with time zone");
        createdAtProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade UpdatedAt (herdada de EntityDefaults)
        var updatedAtProperty = entityType.FindProperty(nameof(ScanEmailDefinition.UpdatedAt));
        _ = updatedAtProperty.ShouldNotBeNull();
        updatedAtProperty.GetColumnName().ShouldBe("updated_at");
        updatedAtProperty.GetColumnType().ShouldBe("timestamp with time zone");
        updatedAtProperty.IsNullable.ShouldBeFalse();
    }
}
