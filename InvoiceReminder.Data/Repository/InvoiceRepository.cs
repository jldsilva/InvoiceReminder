using Dapper;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace InvoiceReminder.Data.Repository;

public class InvoiceRepository : BaseRepository<CoreDbContext, Invoice>, IInvoiceRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<InvoiceRepository> _logger;

    public InvoiceRepository(CoreDbContext dbContext, ILogger<InvoiceRepository> logger) : base(dbContext)
    {
        _dbConnection = dbContext.Database.GetDbConnection();
        _logger = logger;
    }

    public async Task<Invoice> GetByBarcodeAsync(string value, CancellationToken cancellationToken = default)
    {
        Invoice invoice = null;

        try
        {
            var query = @"select * from invoice_reminder.invoice b where rtrim(b.barcode) = @value";
            var command = new CommandDefinition(query, new { value }, cancellationToken: cancellationToken);

            invoice = await _dbConnection.QueryFirstOrDefaultAsync<Invoice>(command);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var contextualInfo = $"GetByBarCodeAsync cancelado para barcode {value}.";

            _logger.LogError(ex, "{ContextualInfo} - Exception: {Message}", contextualInfo, ex.Message);

            throw new InvalidOperationException(contextualInfo, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao consultar invoice por barcode {Barcode}.", value);
        }

        return invoice;
    }
}
