using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Application.Interfaces;
public interface IInvoiceAppService : IBaseAppService<Invoice, InvoiceViewModel>
{
    Task<Result<InvoiceViewModel>> GetByBarcodeAsync(string value);
}
