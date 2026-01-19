using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Data.Interfaces;

public interface IUserPasswordRepository : IBaseRepository<UserPassword>
{
    Task<UserPassword> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
