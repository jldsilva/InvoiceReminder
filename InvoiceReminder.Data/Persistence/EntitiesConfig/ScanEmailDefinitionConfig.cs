using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("InvoiceReminder.Infrastructure.UnitTests")]

namespace InvoiceReminder.Data.Persistence.EntitiesConfig;

internal class ScanEmailDefinitionConfig : IEntityTypeConfiguration<ScanEmailDefinition>
{
    public void Configure(EntityTypeBuilder<ScanEmailDefinition> builder)
    {
        _ = builder.ToTable("scan_email_definition");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd()
            .IsRequired();

        _ = builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        _ = builder.Property(x => x.InvoiceType)
            .HasColumnName("invoice_type")
            .HasConversion<int>()
            .IsRequired();

        _ = builder.Property(x => x.Beneficiary)
            .HasColumnName("beneficiary")
            .HasMaxLength(255)
            .IsRequired();

        _ = builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(255)
            .IsRequired();

        _ = builder.Property(x => x.SenderEmailAddress)
            .HasColumnName("sender_email_address")
            .HasMaxLength(255)
            .IsRequired();

        _ = builder.Property(x => x.AttachmentFileName)
            .HasColumnName("attachment_filename")
            .HasMaxLength(255)
            .IsRequired();

        _ = builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .ValueGeneratedOnAdd()
            .IsRequired();

        _ = builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .ValueGeneratedOnUpdate()
            .IsRequired();
    }
}
