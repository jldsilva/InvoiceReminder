using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("InvoiceReminder.UnitTests.Infrastructure")]

namespace InvoiceReminder.Data.Persistence.EntitiesConfig;

internal class JobScheduleConfig : IEntityTypeConfiguration<JobSchedule>
{
    public void Configure(EntityTypeBuilder<JobSchedule> builder)
    {
        _ = builder.ToTable("job_schedule");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd()
            .IsRequired();

        _ = builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        _ = builder.Property(x => x.CronExpression)
            .HasColumnName("cron_expression")
            .HasMaxLength(255)
            .IsRequired();

        _ = builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        _ = builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
