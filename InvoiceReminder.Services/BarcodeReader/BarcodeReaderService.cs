using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Enums;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;
using System.Text;

namespace InvoiceReminder.ExternalServices.BarcodeReader;

public class BarcodeReaderService : IBarcodeReaderService
{
    private readonly ILogger<BarcodeReaderService> _logger;
    private IInvoiceBarcodeHandler _barcodeHandler;

    public BarcodeReaderService(ILogger<BarcodeReaderService> logger)
    {
        _logger = logger;
    }

    public Invoice ReadTextContentFromPdf(byte[] byteStream, string beneficiary, InvoiceType invoiceType)
    {
        if (byteStream.Length == 0)
        {
            var exception = new ArgumentException("Empty document byte stream", nameof(byteStream));

            _logger.LogError(exception, "{Messagem}", exception.Message);

            throw exception;
        }

        StringBuilder content = new();
        using MemoryStream memory = new(byteStream);
        using PdfReader iTextReader = new(memory);
        using PdfDocument pdfDoc = new(iTextReader);

        var numberofpages = pdfDoc.GetNumberOfPages();

        for (var page = 1; page <= numberofpages; page++)
        {
            ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
            var currentText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page), strategy);

            currentText = Encoding.UTF8.GetString(Encoding.Convert(
                Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(currentText)));

            _ = content.Append(currentText).Replace(" \n", "\n").Replace(" \r\n", "\r\n");
        }

        SetBarcodeHandler(invoiceType);

        return _barcodeHandler.CreateInvoice(content.ToString(), beneficiary);
    }

    private void SetBarcodeHandler(InvoiceType invoiceType)
    {
        _barcodeHandler = invoiceType switch
        {
            InvoiceType.BankInvoice => new BankInvoiceBarcodeHandler(),
            InvoiceType.AccountInvoice => new AccountInvoiceBarcodeHandler(),
            _ => default
        };
    }
}
