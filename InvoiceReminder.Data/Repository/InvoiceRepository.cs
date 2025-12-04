using Dapper;
using InvoiceReminder.Data.Exceptions;
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
    private const string LogExceptionMessage = "{ContextualInfo} - Exception: {Message}";

    public InvoiceRepository(CoreDbContext dbContext, ILogger<InvoiceRepository> logger) : base(dbContext)
    {
        _dbConnection = dbContext.Database.GetDbConnection();
        _logger = logger;
    }

    public async Task<Invoice> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        Invoice invoice = null;

        try
        {
            var query = @"select * from invoice_reminder.invoice i where i.barcode = btrim(@barcode)";
            var command = new CommandDefinition(query, new { barcode }, cancellationToken: cancellationToken);

            invoice = await _dbConnection.QueryFirstOrDefaultAsync<Invoice>(command);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var method = $"{nameof(InvoiceRepository)}.{nameof(GetByBarcodeAsync)}";
            var contextualInfo = $"Method {method} execution was interrupted by a CancellationToken Request...";

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, LogExceptionMessage, contextualInfo, ex.Message);
            }

            throw new OperationCanceledException(contextualInfo, ex, cancellationToken);
        }
        catch (Exception ex)
        {
            var method = $"{nameof(InvoiceRepository)}.{nameof(GetByBarcodeAsync)}";
            var contextualInfo = $"Exception raised while querying DB >> {method}(...)";

            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, LogExceptionMessage, contextualInfo, ex.Message);
            }

            throw new DataLayerException(contextualInfo, ex);
        }

        return invoice;
    }
}
