namespace InvoiceReminder.ExternalServices.SendMessage;

public interface ISendMessageService
{
    Task<string> SendMessageAsync(Guid userId, CancellationToken cancellationToken = default);
}
