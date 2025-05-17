using Dapper;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace InvoiceReminder.Data.Repository;

public class ScanEmailDefinitionRepository : RepositoryBase<CoreDbContext, ScanEmailDefinition>, IScanEmailDefinitionRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<ScanEmailDefinitionRepository> _logger;

    public ScanEmailDefinitionRepository(CoreDbContext dbContext, ILogger<ScanEmailDefinitionRepository> logger) : base(dbContext)
    {
        _dbConnection = dbContext.Database.GetDbConnection();
        _logger = logger;
    }

    public async Task<ScanEmailDefinition> GetBySenderBeneficiaryAsync(string value, Guid id)
    {
        ScanEmailDefinition scanEmailDefinition = null;

        try
        {
            var @params = new { value, id };
            var query = """
                select * from invoice_reminder.scan_email_definition s
                where rtrim(s.beneficiary) = @value
                and s.user_id = @id
                """;

            scanEmailDefinition = await _dbConnection.QueryFirstOrDefaultAsync<ScanEmailDefinition>(query, param: @params);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception raised...");
        }

        return scanEmailDefinition;
    }

    public async Task<ScanEmailDefinition> GetBySenderEmailAddressAsync(string value, Guid id)
    {
        ScanEmailDefinition scanEmailDefinition = null;

        try
        {
            var @params = new { value, id };
            var query = """
                select * from invoice_reminder.scan_email_definition sed
                where rtrim(sed.sender_email_address) = @value
                and sed.user_id = @id
                """;

            scanEmailDefinition = await _dbConnection.QueryFirstOrDefaultAsync<ScanEmailDefinition>(query, param: @params);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception raised...");
        }

        return scanEmailDefinition;
    }

    public async Task<IEnumerable<ScanEmailDefinition>> GetByUserIdAsync(Guid userId)
    {
        IEnumerable<ScanEmailDefinition> scanEmailDefinitions = null;

        try
        {
            var query = """
                select * from invoice_reminder.scan_email_definition s
                where s.user_id = @userId
                """;

            scanEmailDefinitions = await _dbConnection.QueryAsync<ScanEmailDefinition>(query, param: new { userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception raised...");
        }

        return scanEmailDefinitions;
    }
}
