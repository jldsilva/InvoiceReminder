using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.ExternalServices.BarcodeReader;
using InvoiceReminder.ExternalServices.Gmail;
using InvoiceReminder.ExternalServices.Telegram;
using Microsoft.Extensions.Logging;

namespace InvoiceReminder.ExternalServices.SendMessage;

public class SendMessageService : ISendMessageService
{
    private readonly ILogger<SendMessageService> _logger;
    private readonly IBarcodeReaderService _barcodeReaderService;
    private readonly IGmailServiceWrapper _gmailService;
    private readonly ITelegramMessageService _telegramMessageService;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUserRepository _userRepository;

    public SendMessageService(
        IBarcodeReaderService barcodeReaderService,
        IGmailServiceWrapper gmailService,
        ITelegramMessageService telegramMessageService,
        IInvoiceRepository invoiceRepository,
        IUserRepository userRepository,
        ILogger<SendMessageService> logger)
    {
        _barcodeReaderService = barcodeReaderService;
        _gmailService = gmailService;
        _telegramMessageService = telegramMessageService;
        _invoiceRepository = invoiceRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<string> SendMessage(Guid userId)
    {
        IDictionary<string, byte[]> attachments;

        try
        {
            var invoices = new List<Invoice>();
            var user = await _userRepository.GetByIdAsync(userId);
            attachments = await _gmailService.GetAttachmentsAsync(user.Email, user.ScanEmailDefinitions);

            foreach (var attachment in attachments)
            {
                var invoiceType = user.ScanEmailDefinitions
                    .FirstOrDefault(x => x.Beneficiary == attachment.Key).InvoiceType;

                var invoice = _barcodeReaderService.ReadTextContentFromPdf(attachment.Value, attachment.Key, invoiceType);
                invoice.Id = Guid.NewGuid();
                invoice.UserId = userId;
                invoices.Add(invoice);

                var message = $"""
                Um novo boleto de pagamento foi emitido:
                <b>• Emissor:</b> {invoice.Bank}
                <b>• Beneficiário:</b> {invoice.Beneficiary}
                <b>• Cód. Pagamento:</b> {invoice.Barcode}
                <b>• Vencimento:</b> {invoice.DueDate:dd/MM/yyy}
                <b>• Valor:</b> R${invoice.Amount}
                """;

                await _telegramMessageService.SendMessageAsync(user.TelegramChatId, message);
            }

            _ = await _invoiceRepository.BulkInsertAsync(invoices);
        }
        catch (Exception ex)
        {
            var contextualInfo = $"Error occurred while sending messages for userId: {userId}";

            _logger.LogError(ex, "{ContextualInfo} - Exception: {Message}", contextualInfo, ex.Message);

            throw new InvalidOperationException(contextualInfo, ex);
        }

        return $"Total messages sent: {attachments.Count}";
    }
}
