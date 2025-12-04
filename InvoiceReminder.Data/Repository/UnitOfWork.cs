using InvoiceReminder.Data.Exceptions;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace InvoiceReminder.Data.Repository;

public class UnitOfWork : IUnitOfWork
{
    private bool _disposed;
    private readonly CoreDbContext _context;
    private readonly DbConnection _connection;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(CoreDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
        _connection = context.Database.GetDbConnection();
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await OpenConnection(cancellationToken);

        using var transaction = await _connection.BeginTransactionAsync(cancellationToken);

        try
        {
            _ = await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "{Message}", ex.Message);
            }

            await transaction.RollbackAsync(cancellationToken);

            throw new DataLayerException($"Exception raised while saving changes: {ex.Message}", ex);
        }
        finally
        {
            await CloseConnection();
        }
    }

    private async Task OpenConnection(CancellationToken cancellationToken = default)
    {
        if (_connection.State == ConnectionState.Closed)
        {
            await _connection.OpenAsync(cancellationToken);
        }
    }

    private async Task CloseConnection()
    {
        if (_connection.State == ConnectionState.Open)
        {
            await _connection.CloseAsync();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context?.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~UnitOfWork()
    {
        Dispose(false);
    }
}
