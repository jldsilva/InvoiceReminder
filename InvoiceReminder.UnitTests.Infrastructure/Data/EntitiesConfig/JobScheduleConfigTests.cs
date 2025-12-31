using InvoiceReminder.Data.Persistence.EntitiesConfig;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Shouldly;

namespace InvoiceReminder.UnitTests.Infrastructure.Data.EntitiesConfig;

[TestClass]
public sealed class JobScheduleConfigTests
{
    [TestMethod]
    public void JobScheduleConfig_ShouldConfigureEntityCorrectly()
    {
        // Arrange
        var builder = new ModelBuilder(new ConventionSet());
        var config = new JobScheduleConfig();

        // Act
        config.Configure(builder.Entity<JobSchedule>());

        // Assert
        var entityType = builder.Model.FindEntityType(typeof(JobSchedule));
        _ = entityType.ShouldNotBeNull();

        // Verifica tabela
        entityType.GetTableName().ShouldBe("job_schedule");

        // Verifica chave primária
        var primaryKey = entityType.FindPrimaryKey();
        _ = primaryKey.ShouldNotBeNull();
        primaryKey.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(JobSchedule.Id));

        // Verifica propriedade Id
        var idProperty = entityType.FindProperty(nameof(JobSchedule.Id));
        _ = idProperty.ShouldNotBeNull();
        idProperty.GetColumnName().ShouldBe("id");
        idProperty.GetColumnType().ShouldBe("uuid");
        idProperty.IsNullable.ShouldBeFalse();
        idProperty.ValueGenerated.ShouldBe(ValueGenerated.OnAdd);

        // Verifica propriedade UserId
        var userIdProperty = entityType.FindProperty(nameof(JobSchedule.UserId));
        _ = userIdProperty.ShouldNotBeNull();
        userIdProperty.GetColumnName().ShouldBe("user_id");
        userIdProperty.GetColumnType().ShouldBe("uuid");
        userIdProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade CronExpression
        var cronExpressionProperty = entityType.FindProperty(nameof(JobSchedule.CronExpression));
        _ = cronExpressionProperty.ShouldNotBeNull();
        cronExpressionProperty.GetColumnName().ShouldBe("cron_expression");
        cronExpressionProperty.GetMaxLength().ShouldBe(255);
        cronExpressionProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade CreatedAt (herdada de EntityDefaults)
        var createdAtProperty = entityType.FindProperty(nameof(JobSchedule.CreatedAt));
        _ = createdAtProperty.ShouldNotBeNull();
        createdAtProperty.GetColumnName().ShouldBe("created_at");
        createdAtProperty.GetColumnType().ShouldBe("timestamp with time zone");
        createdAtProperty.IsNullable.ShouldBeFalse();

        // Verifica propriedade UpdatedAt (herdada de EntityDefaults)
        var updatedAtProperty = entityType.FindProperty(nameof(JobSchedule.UpdatedAt));
        _ = updatedAtProperty.ShouldNotBeNull();
        updatedAtProperty.GetColumnName().ShouldBe("updated_at");
        updatedAtProperty.GetColumnType().ShouldBe("timestamp with time zone");
        updatedAtProperty.IsNullable.ShouldBeFalse();
    }
}
