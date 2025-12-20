using Bogus;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Enums;
using InvoiceReminder.ExternalServices.BarcodeReader;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.ExternalServices.UnitTests.BarcodeReader;

[TestClass]
public sealed class BarcodeReaderServiceTests
{
    private readonly ILogger<BarcodeReaderService> _logger;
    private readonly IInvoiceBarcodeHandler _barcodeHandler;
    private readonly BarcodeReaderService _barcodeReaderService;
    private readonly Faker<Invoice> _invoiceFaker;
    private readonly Faker _faker;
    private readonly string _accountBarcode;
    private readonly string _bankBarcode;

    public BarcodeReaderServiceTests()
    {
        _logger = Substitute.For<ILogger<BarcodeReaderService>>();
        _barcodeHandler = Substitute.For<IInvoiceBarcodeHandler>();
        _barcodeReaderService = new BarcodeReaderService(_logger);
        _faker = new Faker();

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
            .RuleFor(i => i.DueDate, faker => faker.Date.Future().ToUniversalTime())
            .RuleFor(i => i.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(i => i.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());

        _accountBarcode = "83670000000 0 27540221202 9 50407000000 6 00001559022 7";
        _bankBarcode = "341-7\n34191.09008 14292.880391 20803.050002 4 10770000016165";
    }

    [TestMethod]
    public void ReadTextContentFromPdf_ShouldThrowException_WhenPdfHasInvalidLength()
    {
        // Arrange
        var invalidPdfData = Array.Empty<byte>();
        var beneficiary = _faker.Company.CompanyName();

        // Act && Assert
        _ = Should.Throw<ArgumentException>(() =>
            _barcodeReaderService.ReadTextContentFromPdf(invalidPdfData, beneficiary, InvoiceType.BankInvoice));
    }

    [TestMethod]
    public void ReadTextContentFromPdf_ShouldCreateValidAccountInvoice_WhenPdfContainsBarcode()
    {
        // Arrange
        var pdfData = CreatePdfInMemory(InvoiceType.AccountInvoice);
        var expectedInvoice = _invoiceFaker.Generate();

        _ = _barcodeHandler.CreateInvoice(Arg.Any<string>(), Arg.Any<string>())
            .Returns(expectedInvoice);

        // Act
        var invoice = _barcodeReaderService.ReadTextContentFromPdf(
            pdfData,
            expectedInvoice.Beneficiary,
            InvoiceType.AccountInvoice);

        // Assert
        invoice.ShouldSatisfyAllConditions(invoice =>
        {
            _ = invoice.ShouldNotBeNull();
            invoice.Beneficiary.ShouldBe(expectedInvoice.Beneficiary);
            invoice.Barcode.ShouldNotBeNullOrEmpty();
            invoice.Amount.ShouldBeGreaterThan(0);
        });
    }

    [TestMethod]
    public void ReadTextContentFromPdf_ShouldCreateValidBankInvoice_WhenPdfContainsBarcode()
    {
        // Arrange
        var pdfData = CreatePdfInMemory(InvoiceType.BankInvoice);
        var expectedInvoice = _invoiceFaker.Generate();

        _ = _barcodeHandler.CreateInvoice(Arg.Any<string>(), Arg.Any<string>())
            .Returns(expectedInvoice);

        // Act
        var invoice = _barcodeReaderService.ReadTextContentFromPdf(
            pdfData,
            expectedInvoice.Beneficiary,
            InvoiceType.BankInvoice);

        // Assert
        invoice.ShouldSatisfyAllConditions(invoice =>
        {
            _ = invoice.ShouldNotBeNull();
            invoice.Beneficiary.ShouldBe(expectedInvoice.Beneficiary);
            invoice.Barcode.ShouldNotBeNullOrEmpty();
            invoice.Amount.ShouldBeGreaterThan(0);
        });
    }

    private byte[] CreatePdfInMemory(InvoiceType invoiceType)
    {
        using var memoryStream = new MemoryStream();
        using var pdfWriter = new PdfWriter(memoryStream);
        var pdfDocument = new PdfDocument(pdfWriter);
        var document = new Document(pdfDocument);

        _ = invoiceType == InvoiceType.AccountInvoice
            ? document.Add(new Paragraph(_accountBarcode))
            : document.Add(new Paragraph(_bankBarcode));

        document.Close();

        return memoryStream.ToArray();
    }
}
