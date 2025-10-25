using Dapper;
using InvoiceReminder.Data.Exceptions;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace InvoiceReminder.Data.Repository;

public class UserRepository : BaseRepository<CoreDbContext, User>, IUserRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<UserRepository> _logger;
    private readonly string _query;

    public UserRepository(CoreDbContext dbContext, ILogger<UserRepository> logger) : base(dbContext)
    {
        _dbConnection = dbContext.Database.GetDbConnection();
        _logger = logger;
        _query = """
            select * from invoice_reminder.user u
            left join invoice_reminder.invoice i 
            on u.id = i.user_id
            left join invoice_reminder.job_schedule js
            on u.id = js.user_id
            left join invoice_reminder.email_auth_token eat
            on u.id = eat.user_id
            left join invoice_reminder.scan_email_definition sed
            on u.id = sed.user_id
            """;
    }

    public async Task<User> GetByEmailAsync(string value, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, User>();

        try
        {
            var filter = "where u.email = @value";
            var command = new CommandDefinition($"{_query} {filter}", new { value }, cancellationToken: cancellationToken);

            _ = await _dbConnection.QueryAsync<User, Invoice, JobSchedule, EmailAuthToken, ScanEmailDefinition, User>
                (command, (user, invoice, jobschedule, emailAuthToken, scanEmailDefinition) =>
                {
                    var parameters = new UserParameters
                    {
                        Invoice = invoice,
                        JobSchedule = jobschedule,
                        EmailAuthToken = emailAuthToken,
                        ScanEmailDefinition = scanEmailDefinition
                    };

                    _ = result.Handle(ref user, parameters);

                    return user;
                },
                splitOn: "id");
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var method = $"{nameof(UserRepository)}.{nameof(GetByEmailAsync)}";
            var contextualInfo = $"Method {method} execution was interrupted by a CancellationToken Request...";

            _logger.LogWarning(ex, "{ContextualInfo} - Exception: {Message}", contextualInfo, ex.Message);

            throw new OperationCanceledException(contextualInfo, ex, cancellationToken);
        }
        catch (Exception ex)
        {
            var method = $"{nameof(UserRepository)}.{nameof(GetByEmailAsync)}";
            var contextualInfo = $"Exception raised while querying DB >> {method}(...)";

            _logger.LogError(ex, "{ContextualInfo} - Exception: {Message}", contextualInfo, ex.Message);

            throw new DataLayerException(contextualInfo, ex);
        }

        return result.FirstOrDefault().Value;
    }

    public override async Task<User> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, User>();

        try
        {
            var filter = "where u.id = @id";
            var command = new CommandDefinition($"{_query} {filter}", new { id }, cancellationToken: cancellationToken);

            _ = await _dbConnection.QueryAsync<User, Invoice, JobSchedule, EmailAuthToken, ScanEmailDefinition, User>(
                command, (user, invoice, jobschedule, emailAuthToken, scanEmailDefinition) =>
                {
                    var parameters = new UserParameters
                    {
                        Invoice = invoice,
                        JobSchedule = jobschedule,
                        EmailAuthToken = emailAuthToken,
                        ScanEmailDefinition = scanEmailDefinition
                    };

                    _ = result.Handle(ref user, parameters);

                    return user;
                },
                splitOn: "id");
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var method = $"{nameof(UserRepository)}.{nameof(GetByIdAsync)}";
            var contextualInfo = $"Method {method} execution was interrupted by a CancellationToken Request...";

            _logger.LogWarning(ex, "{ContextualInfo} - Exception: {Message}", contextualInfo, ex.Message);

            throw new OperationCanceledException(contextualInfo, ex, cancellationToken);
        }
        catch (Exception ex)
        {
            var method = $"{nameof(UserRepository)}.{nameof(GetByIdAsync)}";
            var contextualInfo = $"Exception raised while querying DB >> {method}(...)";

            _logger.LogError(ex, "{ContextualInfo} - Exception: {Message}", contextualInfo, ex.Message);

            throw new DataLayerException(contextualInfo, ex);
        }

        return result.FirstOrDefault().Value;
    }
}
