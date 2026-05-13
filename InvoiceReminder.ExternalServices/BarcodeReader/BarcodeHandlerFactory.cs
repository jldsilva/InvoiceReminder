using InvoiceReminder.Domain.Enums;

namespace InvoiceReminder.ExternalServices.BarcodeReader;

public class BarcodeHandlerFactory : IBarcodeHandlerFactory
{
    private readonly Dictionary<InvoiceType, IInvoiceBarcodeHandler> _handlers;

    public BarcodeHandlerFactory(IEnumerable<IInvoiceBarcodeHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.InvoiceType, h => h);
    }

    public IInvoiceBarcodeHandler GetHandler(InvoiceType invoiceType)
    {
        return _handlers.TryGetValue(invoiceType, out var handler)
            ? handler
            : throw new NotSupportedException($"No barcode handler found for invoice type: {invoiceType}");
    }
}
