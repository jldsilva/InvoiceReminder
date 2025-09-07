using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Application.Interfaces;
public interface IScanEmailDefinitionAppService : IBaseAppService<ScanEmailDefinition, ScanEmailDefinitionViewModel>
{
    Task<Result<ScanEmailDefinitionViewModel>> GetBySenderBeneficiaryAsync(string value, Guid id);
    Task<Result<ScanEmailDefinitionViewModel>> GetBySenderEmailAddressAsync(string value, Guid id);
    Task<Result<IEnumerable<ScanEmailDefinitionViewModel>>> GetByUserIdAsync(Guid userId);
}
