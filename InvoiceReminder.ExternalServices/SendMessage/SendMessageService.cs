using InvoiceReminder.Authentication.Extensions;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Services.Configuration;
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
    private readonly string _certificateFilePath;
    private readonly string _certificatePassword;
    private const string LogExceptionMessage = "{ContextualInfo} - Exception: {Message}";

    public SendMessageService(
        IBarcodeReaderService barcodeService,
        IConfigurationService configuration,
        IGmailServiceWrapper gmailService,
        ITelegramMessageService telegramMessageService,
        IInvoiceRepository invoiceRepository,
        IUserRepository userRepository,
        ILogger<SendMessageService> logger)
    {
        var fileName = configuration.GetAppSetting("Security:CertificateFileName");
        var filePath = configuration.GetAppSetting("Security:CertificateFilePath");

        _barcodeService = barcodeService;
        _gmailService = gmailService;
        _telegramMessageService = telegramMessageService;
        _invoiceRepository = invoiceRepository;
        _userRepository = userRepository;
        _logger = logger;
        _certificateFilePath = Path.Combine(filePath, fileName);
        _certificatePassword = configuration.GetAppSetting("Security:CertificatePassword");
    }

    public async Task<string> SendMessageAsync(Guid userId, CancellationToken cancellationToken = default)
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
                var definitions = user.ScanEmailDefinitions?
                    .FirstOrDefault(x => x.SenderEmailAddress == attachment.Key);

                if (definitions is null)
                {
                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogWarning("No ScanEmailDefinition found for attachment: {Beneficiary}", attachment.Key);
                    }

                    continue;
                }

                var filePassword = !string.IsNullOrWhiteSpace(definitions.FilePassword)
                    ? definitions.FilePassword.X509_Decrypt(_certificateFilePath, _certificatePassword)
                    : null;

                var invoice = _barcodeService.ReadTextContentFromPdf(attachment.Value, definitions.Beneficiary,
                    filePassword, definitions.InvoiceType);

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
            var method = $"{nameof(SendMessageService)}.{nameof(SendMessageAsync)}";
            var contextualInfo = $"Method {method} execution was interrupted by a CancellationToken Request...";

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, LogExceptionMessage, contextualInfo, ex.Message);
            }

            throw new OperationCanceledException(contextualInfo, ex, cancellationToken);
        }
        catch (Exception ex)
        {
            var contextualInfo = $"Error occurred while sending messages for userId: {userId}";

            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, LogExceptionMessage, contextualInfo, ex.Message);
            }

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

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("{Message}", message);
            }

            return (false, message);
        }

        if (user.EmailAuthTokens is null || user.EmailAuthTokens.Count == 0)
        {
            message = $"No Authentication Token found for userId: {user.Id}";

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("{Message}", message);
            }

            return (false, message);
        }

        return (true, message);
    }
}
