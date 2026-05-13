using InvoiceReminder.Domain.Enums;

namespace InvoiceReminder.ExternalServices.BarcodeReader;

public interface IBarcodeHandlerFactory
{
    IInvoiceBarcodeHandler GetHandler(InvoiceType invoiceType);
}
