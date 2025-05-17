using Dapper;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace InvoiceReminder.Data.Repository;

public class InvoiceRepository : RepositoryBase<CoreDbContext, Invoice>, IInvoiceRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<InvoiceRepository> _logger;

    public InvoiceRepository(CoreDbContext dbContext, ILogger<InvoiceRepository> logger) : base(dbContext)
    {
        _dbConnection = dbContext.Database.GetDbConnection();
        _logger = logger;
    }

    public async Task<Invoice> GetByBarCodeAsync(string value)
    {
        Invoice invoice = null;

        try
        {
            var query = @"select * from invoice_reminder.invoice b where rtrim(b.barcode) = @value";

            invoice = await _dbConnection.QueryFirstOrDefaultAsync<Invoice>(query, param: new { value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception raised...");
        }

        return invoice;
    }
}
