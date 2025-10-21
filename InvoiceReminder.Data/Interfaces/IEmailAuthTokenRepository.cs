using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Data.Interfaces;

public interface IEmailAuthTokenRepository : IBaseRepository<EmailAuthToken>
{
    Task<EmailAuthToken> GetByUserIdAsync(Guid id, string tokenProvider);
}
