namespace InvoiceReminder.ExternalServices.Telegram;

public interface ITelegramMessageService
{
    Task SendMessageAsync(long chatId, string message, CancellationToken cancellationToken = default);
}
