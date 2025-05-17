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
public class BarcodeReaderServiceTests
{
    private readonly ILogger<BarcodeReaderService> _logger;
    private readonly IInvoiceBarcodeHandler _barcodeHandler;
    private readonly BarcodeReaderService _barcodeReaderService;
    private readonly string _accountBarcode;
    private readonly string _bankBarcode;

    public BarcodeReaderServiceTests()
    {
        _logger = Substitute.For<ILogger<BarcodeReaderService>>();
        _barcodeHandler = Substitute.For<IInvoiceBarcodeHandler>();
        _barcodeReaderService = new BarcodeReaderService(_logger);
        _accountBarcode = "83670000000 0 27540221202 9 50407000000 6 00001559022 7";
        _bankBarcode = "341-7\n34191.09008 14292.880391 20803.050002 4 10770000016165";
    }

    [TestMethod]
    public void ReadTextContentFromPdf_ShouldThrowException_WhenPdfHasInvalidLength()
    {
        // Arrange
        var invalidPdfData = Array.Empty<byte>();
        var beneficiary = "Test Beneficiary";

        // Act && Assert
        _ = Should.Throw<ArgumentException>(() =>
        _barcodeReaderService.ReadTextContentFromPdf(invalidPdfData, beneficiary, InvoiceType.BankInvoice));
    }

    [TestMethod]
    public void ReadTextContentFromPdf_ShouldCreateValidAccountInvoice_WhenPdfContainsBarcode()
    {
        // Arrange
        var pdfData = CreatePdfInMemory(InvoiceType.AccountInvoice);
        var beneficiary = "Test Beneficiary";

        _ = _barcodeHandler.CreateInvoice(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new Invoice
            {
                Beneficiary = beneficiary,
                Barcode = _bankBarcode,
                Amount = 100.00m,
                DueDate = DateTime.Now.AddDays(30)
            });

        // Act
        var invoice = _barcodeReaderService.ReadTextContentFromPdf(pdfData, beneficiary, InvoiceType.AccountInvoice);

        // Assert
        invoice.ShouldSatisfyAllConditions(invoice =>
        {
            _ = invoice.ShouldNotBeNull();
            invoice.Beneficiary.ShouldBe(beneficiary);
            invoice.Barcode.ShouldNotBeNullOrEmpty();
            invoice.Amount.ShouldBeGreaterThan(0);
        });
    }

    [TestMethod]
    public void ReadTextContentFromPdf_ShouldCreateValidBankInvoice_WhenPdfContainsBarcode()
    {
        // Arrange
        var pdfData = CreatePdfInMemory(InvoiceType.BankInvoice);
        var beneficiary = "Test Beneficiary";

        _ = _barcodeHandler.CreateInvoice(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new Invoice
            {
                Beneficiary = beneficiary,
                Barcode = _bankBarcode,
                Amount = 100.00m,
                DueDate = DateTime.Now.AddDays(30)
            });

        // Act
        var invoice = _barcodeReaderService.ReadTextContentFromPdf(pdfData, beneficiary, InvoiceType.BankInvoice);

        // Assert
        invoice.ShouldSatisfyAllConditions(invoice =>
        {
            _ = invoice.ShouldNotBeNull();
            invoice.Beneficiary.ShouldBe(beneficiary);
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
