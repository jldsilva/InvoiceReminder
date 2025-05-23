using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Enums;
using InvoiceReminder.ExternalServices.BarcodeReader;
using InvoiceReminder.ExternalServices.Gmail;
using InvoiceReminder.ExternalServices.SendMessage;
using InvoiceReminder.ExternalServices.Telegram;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.ExternalServices.UnitTests.SendMessage;

[TestClass]
public class SendMessageServiceTests
{
    private readonly ILogger<SendMessageService> _logger;
    private readonly IBarcodeReaderService _barcodeReader;
    private readonly IGmailServiceWrapper _gmailService;
    private readonly ITelegramMessageService _telegramMessageService;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUserRepository _userRepository;
    private readonly SendMessageService _sendMessageService;

    public SendMessageServiceTests()
    {
        _logger = Substitute.For<ILogger<SendMessageService>>();
        _barcodeReader = Substitute.For<IBarcodeReaderService>();
        _gmailService = Substitute.For<IGmailServiceWrapper>();
        _telegramMessageService = Substitute.For<ITelegramMessageService>();
        _invoiceRepository = Substitute.For<IInvoiceRepository>();
        _userRepository = Substitute.For<IUserRepository>();

        _sendMessageService = new SendMessageService(
            _barcodeReader,
            _gmailService,
            _telegramMessageService,
            _invoiceRepository,
            _userRepository,
            _logger
        );
    }

    [TestMethod]
    public async Task SendMessage_Should_Send_Messages_Successfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            TelegramChatId = 12345,
            ScanEmailDefinitions =
            [
                new ScanEmailDefinition
                {
                    AttachmentFileName = "invoice.pdf",
                    Beneficiary = "Test Beneficiary",
                    InvoiceType = InvoiceType.BankInvoice
                }
            ]
        };

        var invoice = new Invoice
        {
            Bank = "Banco Teste",
            Beneficiary = "Teste",
            Barcode = "123456",
            DueDate = DateTime.Today,
            Amount = 100.50m
        };

        var attachments = new Dictionary<string, byte[]> { { "Test Beneficiary", Array.Empty<byte>() } };

        _ = _userRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(Task.FromResult(user));

        _ = _gmailService.GetAttachmentsAsync(Arg.Any<string>(), Arg.Any<ICollection<ScanEmailDefinition>>())
            .Returns(attachments);

        _ = _barcodeReader.ReadTextContentFromPdf(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<InvoiceType>())
            .Returns(invoice);

        // Act
        var result = await _sendMessageService.SendMessage(userId);

        // Assert
        _ = _userRepository.Received(1).GetByIdAsync(Arg.Any<Guid>());
        _ = _gmailService.Received(1).GetAttachmentsAsync(Arg.Any<string>(), Arg.Any<ICollection<ScanEmailDefinition>>());
        _ = _barcodeReader.Received(1).ReadTextContentFromPdf(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<InvoiceType>());
        _ = _telegramMessageService.Received(1).SendMessageAsync(Arg.Any<long>(), Arg.Any<string>());
        _ = _invoiceRepository.Received(1).BulkInsertAsync(Arg.Any<ICollection<Invoice>>());

        result.ShouldSatisfyAllConditions(result =>
        {
            _ = result.ShouldBeOfType<string>();
            result.ShouldNotBeNullOrEmpty();
            result.ShouldBe($"Total messages sent: {attachments.Count}");
        });
    }

    [TestMethod]
    public async Task SendMessage_Should_Log_Error_On_Exception()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var exception = new Exception("User not found");

        _ = _userRepository.GetByIdAsync(userId).Returns(Task.FromException<User>(exception));

        // Act && Assert
        _ = await Should.ThrowAsync<InvalidOperationException>(async () => await _sendMessageService.SendMessage(userId));

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("User not found")),
            exception,
            Arg.Any<Func<object, Exception, string>>()
        );
    }
}
