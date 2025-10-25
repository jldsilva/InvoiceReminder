using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Data.Interfaces;

public interface IScanEmailDefinitionRepository : IBaseRepository<ScanEmailDefinition>
{
    Task<ScanEmailDefinition> GetBySenderBeneficiaryAsync(string beneficiary, Guid id, CancellationToken cancellationToken = default);
    Task<ScanEmailDefinition> GetBySenderEmailAddressAsync(string email, Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ScanEmailDefinition>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
