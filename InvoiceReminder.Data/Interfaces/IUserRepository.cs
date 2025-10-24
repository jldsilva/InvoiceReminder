using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Data.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User> GetByEmailAsync(string value, CancellationToken cancellationToken = default);
    new Task<User> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
