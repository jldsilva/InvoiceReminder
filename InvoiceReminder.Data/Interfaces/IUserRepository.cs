using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Data.Interfaces;

public interface IUserRepository : IRepositoryBase<User>
{
    Task<User> GetByEmailAsync(string value);
    new Task<User> GetByIdAsync(Guid id);
}
