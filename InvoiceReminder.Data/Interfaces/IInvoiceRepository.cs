using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Data.Interfaces;

public interface IInvoiceRepository : IRepositoryBase<Invoice>
{
    Task<Invoice> GetByBarCodeAsync(string value);
}
