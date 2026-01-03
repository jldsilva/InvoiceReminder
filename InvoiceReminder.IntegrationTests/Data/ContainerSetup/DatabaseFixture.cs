using InvoiceReminder.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace InvoiceReminder.IntegrationTests.Data.ContainerSetup;

[TestClass]
public static class DatabaseFixture
{
    private static PostgreSqlContainer _dbContainer;

    public static string ConnectionString => _dbContainer.GetConnectionString();

    [AssemblyInitialize]
    public static async Task AssemblyInit(TestContext context)
    {
        try
        {
            _dbContainer = new PostgreSqlBuilder("postgres:15-alpine")
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("Fake!Password#123")
            .Build();

            await _dbContainer.StartAsync(context.CancellationToken);
            await CreateSchema(context);
            await RunMigrations(context);
        }
        catch (Exception ex)
        {
            context.WriteLine($"Failed to initialize test database: {ex.Message}");
            throw;
        }
    }

    [AssemblyCleanup]
    public static async Task AssemblyCleanup()
    {
        await _dbContainer.DisposeAsync();
    }

    private static async Task CreateSchema(TestContext context)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync(context.CancellationToken);

        await using var cmd = new NpgsqlCommand("CREATE SCHEMA IF NOT EXISTS invoice_reminder;", conn);
        _ = await cmd.ExecuteNonQueryAsync(context.CancellationToken);
    }

    private static async Task RunMigrations(TestContext context)
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        using var dbContext = new CoreDbContext(options);

        await dbContext.Database.MigrateAsync(context.CancellationToken);
    }
}
