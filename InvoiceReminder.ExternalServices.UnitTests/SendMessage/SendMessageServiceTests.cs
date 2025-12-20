using Bogus;
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
public sealed class SendMessageServiceTests
{
    private readonly ILogger<SendMessageService> _logger;
    private readonly IBarcodeReaderService _barcodeReader;
    private readonly IGmailServiceWrapper _gmailService;
    private readonly ITelegramMessageService _telegramMessageService;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUserRepository _userRepository;
    private readonly SendMessageService _sendMessageService;
    private readonly Faker<User> _userFaker;
    private readonly Faker<Invoice> _invoiceFaker;
    private readonly Faker<EmailAuthToken> _emailAuthTokenFaker;
    private readonly Faker<ScanEmailDefinition> _scanEmailDefinitionFaker;

    public TestContext TestContext { get; set; }

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

        _invoiceFaker = new Faker<Invoice>()
            .RuleFor(i => i.Id, faker => faker.Random.Guid())
            .RuleFor(i => i.UserId, faker => faker.Random.Guid())
            .RuleFor(i => i.Bank, faker => faker.PickRandom(
                "Banco do Brasil",
                "Bradesco",
                "Itaú",
                "Caixa Econômica Federal",
                "Santander",
                "Safra",
                "Citibank",
                "BTG Pactual"))
            .RuleFor(i => i.Beneficiary, faker => faker.Company.CompanyName())
            .RuleFor(i => i.Amount, faker => faker.Finance.Amount(10, 10000))
            .RuleFor(i => i.Barcode, faker => faker.Random.AlphaNumeric(47))
            .RuleFor(i => i.DueDate, faker => faker.Date.FutureOffset().DateTime)
            .RuleFor(i => i.CreatedAt, faker => faker.Date.PastOffset().DateTime)
            .RuleFor(i => i.UpdatedAt, faker => faker.Date.RecentOffset().DateTime);

        _emailAuthTokenFaker = new Faker<EmailAuthToken>()
            .RuleFor(e => e.Id, faker => faker.Random.Guid())
            .RuleFor(e => e.UserId, faker => faker.Random.Guid())
            .RuleFor(e => e.AccessToken, faker => faker.Random.AlphaNumeric(256))
            .RuleFor(e => e.RefreshToken, faker => faker.Random.AlphaNumeric(256))
            .RuleFor(e => e.NonceValue, faker => faker.Random.AlphaNumeric(32))
            .RuleFor(e => e.TokenProvider, faker => faker.PickRandom("Google", "Microsoft", "Yahoo"))
            .RuleFor(e => e.AccessTokenExpiry, faker => faker.Date.Future().ToUniversalTime())
            .RuleFor(e => e.CreatedAt, faker => faker.Date.PastOffset().DateTime)
            .RuleFor(e => e.UpdatedAt, faker => faker.Date.RecentOffset().DateTime);

        _scanEmailDefinitionFaker = new Faker<ScanEmailDefinition>()
            .RuleFor(s => s.Id, faker => faker.Random.Guid())
            .RuleFor(s => s.UserId, faker => faker.Random.Guid())
            .RuleFor(s => s.InvoiceType, faker => faker.PickRandom(InvoiceType.AccountInvoice, InvoiceType.BankInvoice))
            .RuleFor(s => s.Beneficiary, faker => faker.Company.CompanyName())
            .RuleFor(s => s.Description, faker => faker.Lorem.Sentence())
            .RuleFor(s => s.SenderEmailAddress, faker => faker.Internet.Email())
            .RuleFor(s => s.AttachmentFileName, faker => faker.System.FileName())
            .RuleFor(s => s.CreatedAt, faker => faker.Date.PastOffset().DateTime)
            .RuleFor(s => s.UpdatedAt, faker => faker.Date.RecentOffset().DateTime);

        _userFaker = new Faker<User>()
            .RuleFor(u => u.Id, faker => faker.Random.Guid())
            .RuleFor(u => u.Name, faker => faker.Person.FullName)
            .RuleFor(u => u.Email, faker => faker.Internet.Email())
            .RuleFor(u => u.Password, faker => faker.Internet.Password())
            .RuleFor(u => u.TelegramChatId, faker => faker.Random.Long(1))
            .RuleFor(u => u.Invoices, [])
            .RuleFor(u => u.JobSchedules, [])
            .RuleFor(u => u.EmailAuthTokens, [])
            .RuleFor(u => u.ScanEmailDefinitions, [])
            .RuleFor(u => u.CreatedAt, faker => faker.Date.PastOffset().DateTime)
            .RuleFor(u => u.UpdatedAt, faker => faker.Date.RecentOffset().DateTime);
    }

    [TestMethod]
    public async Task SendMessage_Should_Send_Messages_Successfully()
    {
        // Arrange
        var authToken = _emailAuthTokenFaker.Generate();
        var scanEmailDefinition = _scanEmailDefinitionFaker.Generate();
        var user = _userFaker
            .Clone()
            .RuleFor(u => u.EmailAuthTokens, new HashSet<EmailAuthToken> { authToken })
            .RuleFor(u => u.ScanEmailDefinitions, new HashSet<ScanEmailDefinition> { scanEmailDefinition })
            .Generate();

        var invoice = _invoiceFaker.Generate();
        var attachments = new Dictionary<string, byte[]> { { scanEmailDefinition.Beneficiary, Array.Empty<byte>() } };

        _ = _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(user));

        _ = _gmailService.GetAttachmentsAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDictionary<string, byte[]>>(attachments));

        _ = _barcodeReader.ReadTextContentFromPdf(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<InvoiceType>())
            .Returns(invoice);

        // Act
        var result = await _sendMessageService.SendMessage(user.Id, TestContext.CancellationToken);

        // Assert
        _ = _userRepository.Received(1).GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        _ = _gmailService.Received(1).GetAttachmentsAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        _ = _barcodeReader.Received(1).ReadTextContentFromPdf(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<InvoiceType>());
        _ = _telegramMessageService.Received(1).SendMessageAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _ = _invoiceRepository.Received(1).BulkInsertAsync(Arg.Any<ICollection<Invoice>>(), Arg.Any<CancellationToken>());

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

        _ = _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<User>(exception));

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act && Assert
        _ = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _sendMessageService.SendMessage(userId, TestContext.CancellationToken)
        );

        var eventId = Arg.Any<EventId>();
        var state = Arg.Is<object>(o => o.ToString().Contains("User not found"));
        var loggedException = Arg.Any<Exception>();
        var formatter = Arg.Any<Func<object, Exception, string>>();

        _logger.Received(1).Log(LogLevel.Error, eventId, state, loggedException, formatter);
    }

    [TestMethod]
    public async Task SendMessage_ShouldNot_SendMessages_When_UserHasNoAuthenticationToken()
    {
        // Arrange
        var user = _userFaker
            .Clone()
            .RuleFor(u => u.EmailAuthTokens, new HashSet<EmailAuthToken>())
            .RuleFor(u => u.ScanEmailDefinitions, new HashSet<ScanEmailDefinition>())
            .Generate();

        _ = _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(user));

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act
        var result = await _sendMessageService.SendMessage(user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldBe($"No Authentication Token found for userId: {user.Id}");

        var eventId = Arg.Any<EventId>();
        var state = Arg.Is<object>(o => o.ToString().Contains("No Authentication Token found"));
        var loggedException = Arg.Is<Exception>(e => e == null);
        var formatter = Arg.Any<Func<object, Exception, string>>();

        _logger.Received(1).Log(LogLevel.Warning, eventId, state, loggedException, formatter);
    }
}
