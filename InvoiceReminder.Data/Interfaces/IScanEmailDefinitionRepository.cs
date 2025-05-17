using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Data.Interfaces;

public interface IScanEmailDefinitionRepository : IRepositoryBase<ScanEmailDefinition>
{
    Task<ScanEmailDefinition> GetBySenderBeneficiaryAsync(string value, Guid id);
    Task<ScanEmailDefinition> GetBySenderEmailAddressAsync(string value, Guid id);
    Task<IEnumerable<ScanEmailDefinition>> GetByUserIdAsync(Guid userId);
}
