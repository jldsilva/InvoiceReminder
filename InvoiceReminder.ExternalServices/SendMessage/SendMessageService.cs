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
    private readonly IBarcodeReaderService _barcodeService;
    private readonly IGmailServiceWrapper _gmailService;
    private readonly ITelegramMessageService _telegramMessageService;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUserRepository _userRepository;

    public SendMessageService(
        IBarcodeReaderService barcodeService,
        IGmailServiceWrapper gmailService,
        ITelegramMessageService telegramMessageService,
        IInvoiceRepository invoiceRepository,
        IUserRepository userRepository,
        ILogger<SendMessageService> logger)
    {
        _barcodeService = barcodeService;
        _gmailService = gmailService;
        _telegramMessageService = telegramMessageService;
        _invoiceRepository = invoiceRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<string> SendMessage(Guid userId, CancellationToken cancellationToken = default)
    {
        IDictionary<string, byte[]> attachments;

        try
        {
            var invoices = new List<Invoice>();
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            var (isValid, validationMessage) = ValidateUser(user);

            if (!isValid)
            {
                return validationMessage;
            }

            attachments = await _gmailService.GetAttachmentsAsync(user, cancellationToken);

            foreach (var attachment in attachments)
            {
                var invoiceType = user.ScanEmailDefinitions?
                    .FirstOrDefault(x => x.Beneficiary == attachment.Key)
                    .InvoiceType;

                var invoice = _barcodeService.ReadTextContentFromPdf(attachment.Value, attachment.Key, invoiceType.Value);
                invoice.Id = Guid.NewGuid();
                invoice.UserId = userId;
                invoices.Add(invoice);

                var message = $"""
                Um novo boleto de pagamento foi emitido:
                <b>• Emissor:</b> {invoice.Bank}
                <b>• Beneficiário:</b> {invoice.Beneficiary}
                <b>• Cód. Pagamento:</b> {invoice.Barcode}
                <b>• Vencimento:</b> {invoice.DueDate:dd/MM/yyyy}
                <b>• Valor:</b> R${invoice.Amount}
                """;

                await _telegramMessageService.SendMessageAsync(user.TelegramChatId, message, cancellationToken);
            }

            _ = await _invoiceRepository.BulkInsertAsync(invoices, cancellationToken);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var method = $"{nameof(SendMessageService)}.{nameof(SendMessage)}";
            var contextualInfo = $"Method {method} execution was interrupted by a CancellationToken Request...";

            _logger.LogWarning(ex, "{ContextualInfo} - Exception: {Message}", contextualInfo, ex.Message);

            throw new OperationCanceledException(contextualInfo, ex, cancellationToken);
        }
        catch (Exception ex)
        {
            var contextualInfo = $"Error occurred while sending messages for userId: {userId}";

            _logger.LogError(ex, "{ContextualInfo} - Exception: {Message}", contextualInfo, ex.Message);

            throw new InvalidOperationException(contextualInfo, ex);
        }

        return $"Total messages sent: {attachments.Count}";
    }

    private (bool, string) ValidateUser(User user)
    {
        var message = string.Empty;

        if (user is null)
        {
            message = $"User not found!";

            _logger.LogWarning("{Message}", message);

            return (false, message);
        }

        if (user.EmailAuthTokens is null || user.EmailAuthTokens.Count == 0)
        {
            message = $"No Authentication Token found for userId: {user.Id}";

            _logger.LogWarning("{Message}", message);

            return (false, message);
        }

        return (true, message);
    }
}
