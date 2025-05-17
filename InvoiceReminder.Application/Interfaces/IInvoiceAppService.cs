using InvoiceReminder.Application.Abstractions;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Application.Interfaces;
public interface IInvoiceAppService : IAppServiceBase<Invoice, InvoiceViewModel>
{
    Task<Result<InvoiceViewModel>> GetByBarcodeAsync(string value);
}
