using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Data.Interfaces;

public interface IInvoiceRepository : IBaseRepository<Invoice>
{
    Task<Invoice> GetByBarcodeAsync(string value, CancellationToken cancellationToken = default);
}
