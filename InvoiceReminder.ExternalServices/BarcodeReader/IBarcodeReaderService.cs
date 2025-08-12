using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Enums;

namespace InvoiceReminder.ExternalServices.BarcodeReader;

public interface IBarcodeReaderService
{
    Invoice ReadTextContentFromPdf(byte[] byteStream, string beneficiary, InvoiceType invoiceType);
}
