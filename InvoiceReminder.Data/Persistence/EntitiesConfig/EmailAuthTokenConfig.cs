using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("InvoiceReminder.Infrastructure.UnitTests")]

namespace InvoiceReminder.Data.Persistence.EntitiesConfig;

internal class EmailAuthTokenConfig : IEntityTypeConfiguration<EmailAuthToken>
{
    public void Configure(EntityTypeBuilder<EmailAuthToken> builder)
    {
        _ = builder.ToTable("email_auth_token");

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

        _ = builder.Property(x => x.AccessToken)
            .HasColumnName("access_token")
            .HasMaxLength(512)
            .IsRequired();

        _ = builder.Property(x => x.RefreshToken)
            .HasColumnName("refresh_token")
            .HasMaxLength(512)
            .IsRequired();

        _ = builder.Property(x => x.NonceValue)
            .HasColumnName("nonce_value")
            .HasMaxLength(64)
            .IsRequired();

        _ = builder.Property(x => x.TokenProvider)
            .HasColumnName("token_provider")
            .HasMaxLength(25)
            .IsRequired();

        _ = builder.Property(x => x.AccessTokenExpiry)
            .HasColumnName("access_token_expiry")
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
