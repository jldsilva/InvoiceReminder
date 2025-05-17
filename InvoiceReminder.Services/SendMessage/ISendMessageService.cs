namespace InvoiceReminder.ExternalServices.SendMessage;

public interface ISendMessageService
{
    Task<string> SendMessage(Guid userId);
}
