using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Enums;

namespace InvoiceReminder.ExternalServices.BarcodeReader;

public interface IInvoiceBarcodeHandler
{
    InvoiceType InvoiceType { get; }

    Invoice CreateInvoice(string content, string beneficiary);
}
