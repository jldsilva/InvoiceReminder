using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.ExternalServices.BarcodeReader;

public interface IInvoiceBarcodeHandler
{
    Invoice CreateInvoice(string content, string beneficiary);
}
