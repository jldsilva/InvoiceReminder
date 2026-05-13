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
    private readonly IBarcodeHandlerFactory _factory;

    public BarcodeReaderService(ILogger<BarcodeReaderService> logger, IBarcodeHandlerFactory factory)
    {
        _logger = logger;
        _factory = factory;
    }

    public Invoice ReadTextContentFromPdf(byte[] byteStream, string beneficiary, string password, InvoiceType invoiceType)
    {
        if (byteStream.Length == 0)
        {
            var exception = new ArgumentException("Empty document byte stream", nameof(byteStream));

            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(exception, "{Messagem}", exception.Message);
            }

            throw exception;
        }

        ReaderProperties props = new();

        if (!string.IsNullOrWhiteSpace(password))
        {
            _ = props.SetPassword(Encoding.UTF8.GetBytes(password));
        }

        using MemoryStream memory = new(byteStream);
        using PdfReader iTextReader = new(memory, props);
        using PdfDocument pdfDoc = new(iTextReader);

        StringBuilder content = new();
        var numberOfPages = pdfDoc.GetNumberOfPages();
        var barcodeHandler = _factory.GetHandler(invoiceType);

        for (var page = 1; page <= numberOfPages; page++)
        {
            ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
            var currentText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page), strategy);

            currentText = Encoding.UTF8.GetString(Encoding.Convert(
                Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(currentText)));

            _ = content.Append(currentText).Replace(" \n", "\n").Replace(" \r\n", "\r\n");
        }

        return barcodeHandler.CreateInvoice(content.ToString(), beneficiary);
    }
}
