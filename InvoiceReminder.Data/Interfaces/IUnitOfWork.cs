namespace InvoiceReminder.Data.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
