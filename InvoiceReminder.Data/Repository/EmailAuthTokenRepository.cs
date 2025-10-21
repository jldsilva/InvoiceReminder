using Dapper;
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

    public async Task<EmailAuthToken> GetByUserIdAsync(Guid id, string tokenProvider)
    {
        EmailAuthToken emailAuthToken = null;

        try
        {
            var parameters = new { id, tokenProvider };
            var query = """
                select * from invoice_reminder.email_auth_token eat
                where eat.user_id = @id and eat.token_provider = @tokenProvider
                """;

            emailAuthToken = await _dbConnection.QueryFirstOrDefaultAsync<EmailAuthToken>(query, param: parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception raised...");
        }

        return emailAuthToken;
    }
}
