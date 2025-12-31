using InvoiceReminder.Data.Exceptions;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
    private const string LogExceptionMessage = "{ContextualInfo} - Exception: {Message}";

    public UnitOfWork(CoreDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
        _connection = context.Database.GetDbConnection();
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        IDbContextTransaction transaction = default;

        try
        {
            await OpenConnection(cancellationToken);

            transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            _ = await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var method = $"{nameof(UnitOfWork)}.{nameof(SaveChangesAsync)}";
            var contextualInfo = $"Method {method} execution was interrupted by a CancellationToken Request...";

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, LogExceptionMessage, contextualInfo, ex.Message);
            }

            throw new OperationCanceledException(contextualInfo, ex, cancellationToken);
        }
        catch (Exception ex)
        {
            var method = $"{nameof(UnitOfWork)}.{nameof(SaveChangesAsync)}";
            var contextualInfo = $"Exception raised. Rolling back changes >> {method}(...)";

            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, LogExceptionMessage, contextualInfo, ex.Message);
            }

            await transaction?.RollbackAsync(CancellationToken.None);

            throw new DataLayerException(contextualInfo, ex);
        }
        finally
        {
            transaction?.Dispose();
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
