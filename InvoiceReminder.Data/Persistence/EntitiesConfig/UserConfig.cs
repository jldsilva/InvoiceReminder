using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("InvoiceReminder.Infrastructure.UnitTests")]

namespace InvoiceReminder.Data.Persistence.EntitiesConfig;

internal class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        _ = builder.ToTable("user");

        _ = builder.HasKey(x => x.Id);

        _ = builder.HasIndex(x => x.Email)
            .HasDatabaseName("idx_user_email")
            .IsUnique();

        _ = builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd()
            .IsRequired();

        _ = builder.Property(x => x.TelegramChatId)
            .HasColumnName("telegram_chat_id")
            .HasColumnType("bigint")
            .HasDefaultValue(0)
            .IsRequired();

        _ = builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        _ = builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        _ = builder.Property(x => x.Password)
            .HasColumnName("password")
            .HasMaxLength(255)
            .IsRequired();

        _ = builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("date")
            .ValueGeneratedOnAdd()
            .IsRequired();

        _ = builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("date")
            .ValueGeneratedOnUpdate()
            .IsRequired();
    }
}
