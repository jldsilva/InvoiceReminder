using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.ExternalServices.Gmail;

public interface IGmailServiceWrapper
{
    Task<IDictionary<string, byte[]>> GetAttachmentsAsync(User user);
}
