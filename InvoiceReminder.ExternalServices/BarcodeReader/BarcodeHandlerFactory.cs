using InvoiceReminder.Domain.Enums;

namespace InvoiceReminder.ExternalServices.BarcodeReader;

public class BarcodeHandlerFactory : IBarcodeHandlerFactory
{
    private readonly Dictionary<InvoiceType, IInvoiceBarcodeHandler> _handlers;

    public BarcodeHandlerFactory(IEnumerable<IInvoiceBarcodeHandler> handlers)
    {
        var duplicatedTypes = handlers
            .GroupBy(h => h.InvoiceType)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (duplicatedTypes.Length > 0)
        {
            throw new InvalidOperationException(
                $"Duplicate barcode handlers registered for: {string.Join(", ", duplicatedTypes)}");
        }

        _handlers = handlers.ToDictionary(h => h.InvoiceType, h => h);
    }

    public IInvoiceBarcodeHandler GetHandler(InvoiceType invoiceType)
    {
        return _handlers.TryGetValue(invoiceType, out var handler)
            ? handler
            : throw new NotSupportedException($"No barcode handler found for invoice type: {invoiceType}");
    }
}
