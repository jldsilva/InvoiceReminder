using Dapper;
using InvoiceReminder.Data.Exceptions;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace InvoiceReminder.Data.Repository;

public class ScanEmailDefinitionRepository : BaseRepository<CoreDbContext, ScanEmailDefinition>, IScanEmailDefinitionRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<ScanEmailDefinitionRepository> _logger;

    public ScanEmailDefinitionRepository(CoreDbContext dbContext, ILogger<ScanEmailDefinitionRepository> logger) : base(dbContext)
    {
        _dbConnection = dbContext.Database.GetDbConnection();
        _logger = logger;
    }

    public async Task<ScanEmailDefinition> GetBySenderBeneficiaryAsync(string value, Guid id, CancellationToken cancellationToken = default)
    {
        ScanEmailDefinition scanEmailDefinition = null;

        try
        {
            var query = """
                select * from invoice_reminder.scan_email_definition s
                where rtrim(s.beneficiary) = @value
                and s.user_id = @id
                """;
            var command = new CommandDefinition(query, new { value, id }, cancellationToken: cancellationToken);

            scanEmailDefinition = await _dbConnection.QueryFirstOrDefaultAsync<ScanEmailDefinition>(command);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var method = $"{nameof(ScanEmailDefinitionRepository)}.{nameof(GetBySenderBeneficiaryAsync)}";
            var contextualInfo = $"Method  {method}  execution was interrupted by a CancellationToken Request...";

            _logger.LogWarning(ex, "{ContextualInfo} - Exception: {Message}", contextualInfo, ex.Message);
        }
        catch (Exception ex)
        {
            var method = $"{nameof(ScanEmailDefinitionRepository)}.{nameof(GetBySenderBeneficiaryAsync)}";
            var contextualInfo = $"Exception raised while querying DB >> {method}(...)";

            _logger.LogError(ex, "{ContextualInfo} - Exception: {Message}", contextualInfo, ex.Message);

            throw new DataLayerException(contextualInfo, ex);
        }

        return scanEmailDefinition;
    }

    public async Task<ScanEmailDefinition> GetBySenderEmailAddressAsync(string value, Guid id, CancellationToken cancellationToken = default)
    {
        ScanEmailDefinition scanEmailDefinition = null;

        try
        {
            var query = """
                select * from invoice_reminder.scan_email_definition sed
                where sed.sender_email_address = btrim(@value)
                and sed.user_id = @id
                """;
            var command = new CommandDefinition(query, new { value, id }, cancellationToken: cancellationToken);

            scanEmailDefinition = await _dbConnection.QueryFirstOrDefaultAsync<ScanEmailDefinition>(command);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var method = $"{nameof(ScanEmailDefinitionRepository)}.{nameof(GetBySenderEmailAddressAsync)}";
            var contextualInfo = $"Method  {method}  execution was interrupted by a CancellationToken Request...";

            _logger.LogWarning(ex, "{ContextualInfo} - Exception: {Message}", contextualInfo, ex.Message);
        }
        catch (Exception ex)
        {
            var method = $"{nameof(ScanEmailDefinitionRepository)}.{nameof(GetBySenderEmailAddressAsync)}";
            var contextualInfo = $"Exception raised while querying DB >> {method}(...)";

            _logger.LogError(ex, "{ContextualInfo} - Exception: {Message}", contextualInfo, ex.Message);

            throw new DataLayerException(contextualInfo, ex);
        }

        return scanEmailDefinition;
    }

    public async Task<IEnumerable<ScanEmailDefinition>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IEnumerable<ScanEmailDefinition> scanEmailDefinitions = null;

        try
        {
            var query = """
                select * from invoice_reminder.scan_email_definition s
                where s.user_id = @userId
                """;
            var command = new CommandDefinition(query, new { userId }, cancellationToken: cancellationToken);

            scanEmailDefinitions = await _dbConnection.QueryAsync<ScanEmailDefinition>(command);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var method = $"{nameof(ScanEmailDefinitionRepository)}.{nameof(GetByUserIdAsync)}";
            var contextualInfo = $"Method {method} execution was interrupted by a CancellationToken Request...";

            _logger.LogWarning(ex, "{ContextualInfo} - Exception: {Message}", contextualInfo, ex.Message);

            throw new DataLayerException(contextualInfo, ex);
        }
        catch (Exception ex)
        {
            var method = $"{nameof(ScanEmailDefinitionRepository)}.{nameof(GetByUserIdAsync)}";
            var contextualInfo = $"Exception raised while querying DB >> {method}(...)";

            _logger.LogError(ex, "{ContextualInfo} - Exception: {Message}", contextualInfo, ex.Message);

            throw new DataLayerException(contextualInfo, ex);
        }

        return scanEmailDefinitions;
    }
}
