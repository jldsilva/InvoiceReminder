using Dapper;
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
            left join scan_email_definition sed
            on u.id = sed.user_id
            """;
    }

    public async Task<User> GetByEmailAsync(string value)
    {
        var result = new Dictionary<Guid, User>();

        try
        {
            var filter = "where u.email = @value";

            _ = await _dbConnection.QueryAsync<User, Invoice, JobSchedule, ScanEmailDefinition, EmailAuthToken, User>
                ($"{_query} {filter}", (user, invoice, jobschedule, scanEmailDefinition, emailAuthToken) =>
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
                param: new { value },
                splitOn: "id");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception raised...");
        }

        return result.FirstOrDefault().Value;
    }

    public override async Task<User> GetByIdAsync(Guid id)
    {
        var result = new Dictionary<Guid, User>();

        try
        {
            var filter = "where u.id = @id";

            _ = await _dbConnection.QueryAsync<User, Invoice, JobSchedule, EmailAuthToken, ScanEmailDefinition, User>
                ($"{_query} {filter}", (user, invoice, jobschedule, emailAuthToken, scanEmailDefinition) =>
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
                param: new { id },
                splitOn: "id");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception raised...");
        }

        return result.FirstOrDefault().Value;
    }
}
