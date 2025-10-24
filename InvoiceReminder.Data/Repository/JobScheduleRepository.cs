using Dapper;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace InvoiceReminder.Data.Repository;

public class JobScheduleRepository : BaseRepository<CoreDbContext, JobSchedule>, IJobScheduleRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<JobScheduleRepository> _logger;

    public JobScheduleRepository(CoreDbContext dbContext, ILogger<JobScheduleRepository> logger) : base(dbContext)
    {
        _dbConnection = dbContext.Database.GetDbConnection();
        _logger = logger;
    }

    public async Task<IEnumerable<JobSchedule>> GetByUserIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var jobSchedule = Enumerable.Empty<JobSchedule>();

        try
        {
            var query = @"select * from invoice_reminder.job_schedule j where j.user_id = @id";
            var command = new CommandDefinition(query, new { id }, cancellationToken: cancellationToken);

            jobSchedule = await _dbConnection.QueryAsync<JobSchedule>(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception raised...");
        }

        return jobSchedule;
    }
}
