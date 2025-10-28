using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("InvoiceReminder.Infrastructure.UnitTests")]

namespace InvoiceReminder.Data.Persistence.EntitiesConfig;

internal class InvoiceConfig : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        _ = builder.ToTable("invoice");

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

        _ = builder.Property(x => x.Bank)
            .HasColumnName("bank")
            .IsRequired()
            .HasMaxLength(255);

        _ = builder.Property(x => x.Beneficiary)
            .HasColumnName("beneficiary")
            .IsRequired()
            .HasMaxLength(255);

        _ = builder.Property(x => x.Barcode)
            .HasColumnName("barcode")
            .IsRequired();

        _ = builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .IsRequired();

        _ = builder.Property(x => x.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("timestamp with time zone")
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
