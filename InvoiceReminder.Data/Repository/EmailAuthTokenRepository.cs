using Dapper;
using InvoiceReminder.Data.Exceptions;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace InvoiceReminder.Data.Repository;

public class EmailAuthTokenRepository : BaseRepository<CoreDbContext, EmailAuthToken>, IEmailAuthTokenRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<EmailAuthTokenRepository> _logger;

    public EmailAuthTokenRepository(CoreDbContext dbContext, ILogger<EmailAuthTokenRepository> logger) : base(dbContext)
    {
        _dbConnection = dbContext.Database.GetDbConnection();
        _logger = logger;
    }

    public async Task<EmailAuthToken> GetByUserIdAsync(Guid id, string tokenProvider, CancellationToken cancellationToken = default)
    {
        EmailAuthToken emailAuthToken = null;

        try
        {
            var query = """
                select * from invoice_reminder.email_auth_token eat
                where eat.user_id = @id and eat.token_provider = @tokenProvider
                """;
            var command = new CommandDefinition(query, new { id, tokenProvider }, cancellationToken: cancellationToken);

            emailAuthToken = await _dbConnection.QueryFirstOrDefaultAsync<EmailAuthToken>(command);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var method = $"{nameof(EmailAuthTokenRepository)}.{nameof(GetByUserIdAsync)}";
            var contextualInfo = $"Method {method} execution was interrupted by a CancellationToken Request...";

            _logger.LogWarning(ex, "{ContextualInfo} - Exception: {Message}", contextualInfo, ex.Message);
        }
        catch (Exception ex)
        {
            var method = $"{nameof(EmailAuthTokenRepository)}.{nameof(GetByUserIdAsync)}";
            var contextualInfo = $"Exception raised while querying DB >> {method}(...)";

            _logger.LogError(ex, "{ContextualInfo} - Exception: {Message}", contextualInfo, ex.Message);

            throw new DataLayerException(contextualInfo, ex);
        }

        return emailAuthToken;
    }
}
