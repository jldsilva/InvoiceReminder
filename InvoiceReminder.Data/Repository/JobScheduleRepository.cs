using Dapper;
using InvoiceReminder.Data.Exceptions;
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
    private const string LogExceptionMessage = "{ContextualInfo} - Exception: {Message}";

    public JobScheduleRepository(CoreDbContext dbContext, ILogger<JobScheduleRepository> logger) : base(dbContext)
    {
        _dbConnection = dbContext.Database.GetDbConnection();
        _logger = logger;
    }

    public async Task<IEnumerable<JobSchedule>> GetByUserIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        IEnumerable<JobSchedule> jobSchedule;

        try
        {
            var query = @"select * from invoice_reminder.job_schedule j where j.user_id = @id";
            var command = new CommandDefinition(query, new { id }, cancellationToken: cancellationToken);

            jobSchedule = await _dbConnection.QueryAsync<JobSchedule>(command);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var method = $"{nameof(JobScheduleRepository)}.{nameof(GetByUserIdAsync)}";
            var contextualInfo = $"Method {method} execution was interrupted by a CancellationToken Request...";

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, LogExceptionMessage, contextualInfo, ex.Message);
            }

            throw new OperationCanceledException(contextualInfo, ex, cancellationToken);
        }
        catch (Exception ex)
        {
            var method = $"{nameof(JobScheduleRepository)}.{nameof(GetByUserIdAsync)}";
            var contextualInfo = $"Exception raised while querying DB >> {method}(...)";

            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, LogExceptionMessage, contextualInfo, ex.Message);
            }

            throw new DataLayerException(contextualInfo, ex);
        }

        return jobSchedule;
    }
}
