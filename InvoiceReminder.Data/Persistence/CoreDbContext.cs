using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceReminder.Data.Persistence;

public class CoreDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<JobSchedule> Schedules => Set<JobSchedule>();
    public DbSet<EmailAuthToken> EmailAuthTokens => Set<EmailAuthToken>();
    public DbSet<ScanEmailDefinition> ScanEmailDefinitions => Set<ScanEmailDefinition>();

    public CoreDbContext() { }

    public CoreDbContext(DbContextOptions<CoreDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.HasDefaultSchema("invoice_reminder");
        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoreDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is EntityDefaults
                && (e.State == EntityState.Added
                || e.State == EntityState.Modified)
            );

        foreach (var entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Added)
            {
                ((EntityDefaults)entityEntry.Entity).CreatedAt = DateTime.UtcNow;
                ((EntityDefaults)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
            }

            if (entityEntry.State == EntityState.Modified)
            {
                ((EntityDefaults)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
