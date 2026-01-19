using Dapper;
using InvoiceReminder.Data.Exceptions;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace InvoiceReminder.Data.Repository;

public class UserPasswordRepository : BaseRepository<CoreDbContext, UserPassword>, IUserPasswordRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<UserPasswordRepository> _logger;
    private const string LogExceptionMessage = "{ContextualInfo} - Exception: {Message}";

    public UserPasswordRepository(CoreDbContext dbContext, ILogger<UserPasswordRepository> logger) : base(dbContext)
    {
        _dbConnection = dbContext.Database.GetDbConnection();
        _logger = logger;
    }

    public async Task<UserPassword> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        UserPassword userPassword = default;

        try
        {
            var query = @"select * from invoice_reminder.user_password up where up.user_id = @userid";
            var command = new CommandDefinition(query, new { userId }, cancellationToken: cancellationToken);

            userPassword = await _dbConnection.QueryFirstOrDefaultAsync<UserPassword>(command);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var method = $"{nameof(UserPasswordRepository)}.{nameof(GetByUserIdAsync)}";
            var contextualInfo = $"Method {method} execution was interrupted by a CancellationToken Request...";

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, LogExceptionMessage, contextualInfo, ex.Message);
            }

            throw new OperationCanceledException(contextualInfo, ex, cancellationToken);
        }
        catch (Exception ex)
        {
            var method = $"{nameof(UserPasswordRepository)}.{nameof(GetByUserIdAsync)}";
            var contextualInfo = $"Exception raised while querying DB >> {method}(...)";

            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, LogExceptionMessage, contextualInfo, ex.Message);
            }

            throw new DataLayerException(contextualInfo, ex);
        }

        return userPassword;
    }
}
