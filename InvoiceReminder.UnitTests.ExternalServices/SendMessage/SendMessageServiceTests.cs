using Bogus;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Enums;
using InvoiceReminder.Domain.Services.Configuration;
using InvoiceReminder.ExternalServices.BarcodeReader;
using InvoiceReminder.ExternalServices.Gmail;
using InvoiceReminder.ExternalServices.SendMessage;
using InvoiceReminder.ExternalServices.Telegram;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace InvoiceReminder.UnitTests.ExternalServices.SendMessage;

[TestClass]
public sealed class SendMessageServiceTests
{
    private readonly ILogger<SendMessageService> _logger;
    private readonly IBarcodeReaderService _barcodeReader;
    private readonly IConfigurationService _configuration;
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
        _configuration = Substitute.For<IConfigurationService>();
        _gmailService = Substitute.For<IGmailServiceWrapper>();
        _telegramMessageService = Substitute.For<ITelegramMessageService>();
        _invoiceRepository = Substitute.For<IInvoiceRepository>();
        _userRepository = Substitute.For<IUserRepository>();

        _sendMessageService = new SendMessageService(
            _barcodeReader,
            _configuration,
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
            .RuleFor(s => s.FilePassword, faker => null)
            .RuleFor(s => s.CreatedAt, faker => faker.Date.PastOffset().DateTime)
            .RuleFor(s => s.UpdatedAt, faker => faker.Date.RecentOffset().DateTime);

        _userFaker = new Faker<User>()
            .RuleFor(u => u.Id, faker => faker.Random.Guid())
            .RuleFor(u => u.Name, faker => faker.Person.FullName)
            .RuleFor(u => u.Email, faker => faker.Internet.Email())
            .RuleFor(u => u.TelegramChatId, faker => faker.Random.Long(1))
            .RuleFor(u => u.Invoices, [])
            .RuleFor(u => u.JobSchedules, [])
            .RuleFor(u => u.EmailAuthTokens, [])
            .RuleFor(u => u.ScanEmailDefinitions, [])
            .RuleFor(u => u.CreatedAt, faker => faker.Date.PastOffset().DateTime)
            .RuleFor(u => u.UpdatedAt, faker => faker.Date.RecentOffset().DateTime);
    }

    #region SendMessage_Success Tests

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
        var attachments = new Dictionary<string, byte[]>
        {
            { scanEmailDefinition.SenderEmailAddress, Array.Empty<byte>() }
        };

        _ = _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(user));

        _ = _gmailService.GetAttachmentsAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDictionary<string, byte[]>>(attachments));

        _ = _barcodeReader
            .ReadTextContentFromPdf(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<InvoiceType>())
            .Returns(invoice);

        // Act
        var result = await _sendMessageService.SendMessageAsync(user.Id, TestContext.CancellationToken);

        // Assert
        _ = _userRepository.Received(1)
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        _ = _gmailService.Received(1)
            .GetAttachmentsAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());

        _ = _barcodeReader.Received(1)
            .ReadTextContentFromPdf(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<InvoiceType>());

        _ = _telegramMessageService.Received(1)
            .SendMessageAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        _ = _invoiceRepository.Received(1)
            .BulkInsertAsync(Arg.Any<ICollection<Invoice>>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(result =>
        {
            _ = result.ShouldBeOfType<string>();
            result.ShouldNotBeNullOrEmpty();
            result.ShouldBe($"Total messages sent: {attachments.Count}");
        });
    }

    [TestMethod]
    public async Task SendMessage_With_MultipleAttachments_Should_Send_All_Messages()
    {
        // Arrange
        var authToken = _emailAuthTokenFaker.Generate();
        var scanEmailDefinition1 = _scanEmailDefinitionFaker.Generate();
        var scanEmailDefinition2 = _scanEmailDefinitionFaker.Generate();

        var user = _userFaker
            .Clone()
            .RuleFor(u => u.EmailAuthTokens, new HashSet<EmailAuthToken> { authToken })
            .RuleFor(u => u.ScanEmailDefinitions, new HashSet<ScanEmailDefinition> { scanEmailDefinition1, scanEmailDefinition2 })
            .Generate();

        var invoice1 = _invoiceFaker.Generate();
        var invoice2 = _invoiceFaker.Generate();

        var attachments = new Dictionary<string, byte[]>
        {
            { scanEmailDefinition1.SenderEmailAddress, Array.Empty<byte>() },
            { scanEmailDefinition2.SenderEmailAddress, Array.Empty<byte>() }
        };

        _ = _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(user));

        _ = _gmailService.GetAttachmentsAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDictionary<string, byte[]>>(attachments));

        _ = _barcodeReader
            .ReadTextContentFromPdf(Arg.Any<byte[]>(), scanEmailDefinition1.Beneficiary, Arg.Any<string>(), Arg.Any<InvoiceType>())
            .Returns(invoice1);

        _ = _barcodeReader
            .ReadTextContentFromPdf(Arg.Any<byte[]>(), scanEmailDefinition2.Beneficiary, Arg.Any<string>(), Arg.Any<InvoiceType>())
            .Returns(invoice2);

        // Act
        var result = await _sendMessageService.SendMessageAsync(user.Id, TestContext.CancellationToken);

        // Assert
        _ = _barcodeReader.Received(2)
            .ReadTextContentFromPdf(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<InvoiceType>());

        _ = _telegramMessageService.Received(2)
            .SendMessageAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        result.ShouldBe($"Total messages sent: {attachments.Count}");
    }

    [TestMethod]
    public async Task SendMessage_With_EmptyAttachments_Should_Return_Zero_Messages()
    {
        // Arrange
        var authToken = _emailAuthTokenFaker.Generate();
        var user = _userFaker
            .Clone()
            .RuleFor(u => u.EmailAuthTokens, new HashSet<EmailAuthToken> { authToken })
            .RuleFor(u => u.ScanEmailDefinitions, new HashSet<ScanEmailDefinition>())
            .Generate();

        var attachments = new Dictionary<string, byte[]>();

        _ = _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(user));

        _ = _gmailService.GetAttachmentsAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDictionary<string, byte[]>>(attachments));

        // Act
        var result = await _sendMessageService.SendMessageAsync(user.Id, TestContext.CancellationToken);

        // Assert
        _ = _barcodeReader.DidNotReceive()
            .ReadTextContentFromPdf(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<InvoiceType>());

        _ = _telegramMessageService.DidNotReceive()
            .SendMessageAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        result.ShouldBe("Total messages sent: 0");
    }

    [TestMethod]
    public async Task SendMessage_Should_Set_Invoice_Properties_Correctly()
    {
        // Arrange
        var authToken = _emailAuthTokenFaker.Generate();
        var scanEmailDefinition = _scanEmailDefinitionFaker.Generate();
        var user = _userFaker
            .Clone()
            .RuleFor(u => u.EmailAuthTokens, new HashSet<EmailAuthToken> { authToken })
            .RuleFor(u => u.ScanEmailDefinitions, new HashSet<ScanEmailDefinition> { scanEmailDefinition })
            .Generate();

        var generatedInvoice = _invoiceFaker.Generate();
        var attachments = new Dictionary<string, byte[]>
        {
            { scanEmailDefinition.SenderEmailAddress, Array.Empty<byte>() }
        };

        _ = _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(user));

        _ = _gmailService.GetAttachmentsAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDictionary<string, byte[]>>(attachments));

        _ = _barcodeReader
            .ReadTextContentFromPdf(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<InvoiceType>())
            .Returns(generatedInvoice);

        // Act
        _ = await _sendMessageService.SendMessageAsync(user.Id, TestContext.CancellationToken);

        // Assert
        _ = _invoiceRepository.Received(1).BulkInsertAsync(
            Arg.Is<ICollection<Invoice>>(invoices =>
                invoices.Count == 1 &&
                invoices.First().UserId == user.Id &&
                invoices.First().Bank == generatedInvoice.Bank &&
                invoices.First().Beneficiary == generatedInvoice.Beneficiary &&
                invoices.First().Id != Guid.Empty),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SendMessage_With_EmptyFilePassword_Should_Pass_Null_To_BarcodeReader()
    {
        // Arrange
        // This test validates that when FilePassword is null or empty, null is passed to BarcodeReader
        var authToken = _emailAuthTokenFaker.Generate();
        var scanEmailDefinition = _scanEmailDefinitionFaker
            .Clone()
            .RuleFor(s => s.FilePassword, (Faker faker) => null)
            .Generate();

        var user = _userFaker
            .Clone()
            .RuleFor(u => u.EmailAuthTokens, new HashSet<EmailAuthToken> { authToken })
            .RuleFor(u => u.ScanEmailDefinitions, new HashSet<ScanEmailDefinition> { scanEmailDefinition })
            .Generate();

        var generatedInvoice = _invoiceFaker.Generate();
        var attachments = new Dictionary<string, byte[]>
        {
            { scanEmailDefinition.SenderEmailAddress, Array.Empty<byte>() }
        };

        _ = _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(user));

        _ = _gmailService.GetAttachmentsAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDictionary<string, byte[]>>(attachments));

        var definitions = user.ScanEmailDefinitions.First();

        _ = _barcodeReader.ReadTextContentFromPdf(
            Arg.Any<byte[]>(),
            scanEmailDefinition.Beneficiary,
            Arg.Is<string>(p => p == null),
            definitions.InvoiceType)
            .Returns(generatedInvoice);

        // Act
        var result = await _sendMessageService.SendMessageAsync(user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldBe($"Total messages sent: {attachments.Count}");

        // Verify barcodeReader was called with null password parameter
        _ = _barcodeReader.Received(1).ReadTextContentFromPdf(
            Arg.Any<byte[]>(),
            scanEmailDefinition.Beneficiary,
            Arg.Is<string>(p => p == null),
            definitions.InvoiceType);

        _ = _invoiceRepository.Received(1).BulkInsertAsync(
            Arg.Any<ICollection<Invoice>>(),
            Arg.Any<CancellationToken>());

        _ = _telegramMessageService.Received(1).SendMessageAsync(
            Arg.Any<long>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SendMessage_With_NonEmptyFilePassword_Should_Attempt_Decryption()
    {
        // Arrange
        // This test validates that when FilePassword is non-empty, the X509_Decrypt path is attempted
        // With an invalid certificate file path, the decryption will fail as expected
        var authToken = _emailAuthTokenFaker.Generate();
        var scanEmailDefinition = _scanEmailDefinitionFaker
            .Clone()
            .RuleFor(s => s.FilePassword, faker => "EncryptedPasswordValue")
            .Generate();

        var user = _userFaker
            .Clone()
            .RuleFor(u => u.EmailAuthTokens, new HashSet<EmailAuthToken> { authToken })
            .RuleFor(u => u.ScanEmailDefinitions, new HashSet<ScanEmailDefinition> { scanEmailDefinition })
            .Generate();

        var attachments = new Dictionary<string, byte[]> { { scanEmailDefinition.SenderEmailAddress, Array.Empty<byte>() } };

        // Mock configuration to return an invalid/nonexistent certificate file path
        var invalidCertPath = Path.Combine(Path.GetTempPath(), "nonexistent_certificate_12345.pfx");
        var configurationMock = Substitute.For<IConfigurationService>();
        _ = configurationMock.GetAppSetting("Security:CertificateFilePath").Returns(invalidCertPath);
        _ = configurationMock.GetAppSetting("Security:CertificatePassword").Returns((string)null);

        // Create a new service with the mocked configuration
        var sendMessageService = new SendMessageService(
            _barcodeReader,
            configurationMock,
            _gmailService,
            _telegramMessageService,
            _invoiceRepository,
            _userRepository,
            _logger
        );

        _ = _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(user));

        _ = _gmailService.GetAttachmentsAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDictionary<string, byte[]>>(attachments));

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        // The X509_Decrypt will fail because the certificate file doesn't exist
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sendMessageService.SendMessageAsync(user.Id, TestContext.CancellationToken)
        );

        exception.Message.ShouldContain("Error occurred while sending messages");

        // Verify error was logged (FileNotFoundException from missing certificate file)
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception>(ex =>
                (ex is FileNotFoundException && ex.Message.Contains("Certificado")) ||
                (ex.InnerException is FileNotFoundException &&
                 ex.InnerException.Message.Contains("Certificado"))
            ),
            Arg.Any<Func<object, Exception, string>>()
        );

        // BarcodeReader should not be called because decryption failed first
        _ = _barcodeReader.DidNotReceive().ReadTextContentFromPdf(
            Arg.Any<byte[]>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<InvoiceType>());
    }

    #endregion

    #region SendMessage_Validation Tests

    [TestMethod]
    public async Task SendMessage_ShouldNot_SendMessages_When_UserIsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _ = _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User>(null));

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act
        var result = await _sendMessageService.SendMessageAsync(userId, TestContext.CancellationToken);

        // Assert
        result.ShouldBe("User not found!");

        _ = _gmailService.DidNotReceive()
            .GetAttachmentsAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());

        _ = _barcodeReader.DidNotReceive()
            .ReadTextContentFromPdf(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<InvoiceType>());

        _ = _telegramMessageService.DidNotReceive()
            .SendMessageAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("User not found!")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );
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
        var result = await _sendMessageService.SendMessageAsync(user.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldBe($"No Authentication Token found for userId: {user.Id}");

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("No Authentication Token found")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    #endregion

    #region SendMessage_Exception Tests

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
            await _sendMessageService.SendMessageAsync(userId, TestContext.CancellationToken)
        );

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("User not found")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    [TestMethod]
    public async Task SendMessage_GmailService_ThrowsException_Should_Log_And_Rethrow()
    {
        // Arrange
        var authToken = _emailAuthTokenFaker.Generate();
        var user = _userFaker
            .Clone()
            .RuleFor(u => u.EmailAuthTokens, new HashSet<EmailAuthToken> { authToken })
            .Generate();

        _ = _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(user));

        _ = _gmailService.GetAttachmentsAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Gmail service error"));

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act && Assert
        _ = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _sendMessageService.SendMessageAsync(user.Id, TestContext.CancellationToken)
        );

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    [TestMethod]
    public async Task SendMessage_BarcodeReader_ThrowsException_Should_Log_And_Rethrow()
    {
        // Arrange
        var authToken = _emailAuthTokenFaker.Generate();
        var scanEmailDefinition = _scanEmailDefinitionFaker.Generate();
        var user = _userFaker
            .Clone()
            .RuleFor(u => u.EmailAuthTokens, new HashSet<EmailAuthToken> { authToken })
            .RuleFor(u => u.ScanEmailDefinitions, new HashSet<ScanEmailDefinition> { scanEmailDefinition })
            .Generate();

        var attachments = new Dictionary<string, byte[]> { { scanEmailDefinition.SenderEmailAddress, Array.Empty<byte>() } };

        _ = _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(user));

        _ = _gmailService.GetAttachmentsAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDictionary<string, byte[]>>(attachments));

        _ = _barcodeReader.ReadTextContentFromPdf(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<InvoiceType>())
            .Throws(new Exception("Barcode reader error"));

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act && Assert
        _ = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _sendMessageService.SendMessageAsync(user.Id, TestContext.CancellationToken)
        );

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    [TestMethod]
    public async Task SendMessage_OperationCanceledException_Should_Log_Warning_And_Rethrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        await cts.CancelAsync();

        _ = _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException("Operation was cancelled", null, cts.Token));

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act && Assert
        _ = await Should.ThrowAsync<OperationCanceledException>(async () =>
            await _sendMessageService.SendMessageAsync(userId, cts.Token)
        );

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    [TestMethod]
    public async Task SendMessage_TelegramService_ThrowsException_Should_Log_And_Rethrow()
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
        var attachments = new Dictionary<string, byte[]> { { scanEmailDefinition.SenderEmailAddress, Array.Empty<byte>() } };

        _ = _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(user));

        _ = _gmailService.GetAttachmentsAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDictionary<string, byte[]>>(attachments));

        _ = _barcodeReader.ReadTextContentFromPdf(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<InvoiceType>())
            .Returns(invoice);

        _ = _telegramMessageService.SendMessageAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Telegram service error"));

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act && Assert
        _ = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _sendMessageService.SendMessageAsync(user.Id, TestContext.CancellationToken)
        );

        _ = _invoiceRepository.DidNotReceive()
            .BulkInsertAsync(Arg.Any<ICollection<Invoice>>(), Arg.Any<CancellationToken>());

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    #endregion
}
