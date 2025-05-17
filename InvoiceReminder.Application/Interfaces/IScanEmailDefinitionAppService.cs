using InvoiceReminder.Application.Abstractions;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Application.Interfaces;
public interface IScanEmailDefinitionAppService : IAppServiceBase<ScanEmailDefinition, ScanEmailDefinitionViewModel>
{
    Task<Result<ScanEmailDefinitionViewModel>> GetBySenderBeneficiaryAsync(string value, Guid id);
    Task<Result<ScanEmailDefinitionViewModel>> GetBySenderEmailAddressAsync(string value, Guid id);
    Task<Result<IEnumerable<ScanEmailDefinitionViewModel>>> GetByUserIdAsync(Guid userId);
}
