using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Data.Interfaces;

public interface IUserPasswordRepository : IBaseRepository<UserPassword>
{
    Task<UserPassword> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(UserPassword userPassword, CancellationToken cancellationToken = default);
}
