using InvoiceReminder.Domain.Entities;
using System.Text.RegularExpressions;

namespace InvoiceReminder.ExternalServices.BarcodeReader;

public class AccountInvoiceBarcodeHandler : IInvoiceBarcodeHandler
{
    public Invoice CreateInvoice(string content, string beneficiary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var barcode = FilterContent(content);

        return new Invoice
        {
            Bank = beneficiary,
            Beneficiary = beneficiary,
            Amount = GetPaymentValue(barcode),
            Barcode = barcode,
            DueDate = GetPaymentDueDate(barcode)
        };
    }

    private static string FilterContent(string content)
    {
        var pattern = @"(\d{11}\s\d)\s(\d{11}\s\d)\s(\d{11}\s\d)\s(\d{11}\s\d)";
        var match = Regex.Match(content, pattern);

        return match.Groups[0].Value;
    }

    private static DateTime GetPaymentDueDate(string content)
    {
        var value = content.Replace(" ", "");
        var baseDateCode = value.Substring(24, 6);
        var year = 2000 + (int.Parse(baseDateCode[..2]) / 2);
        var month = int.Parse(new string([.. baseDateCode[2..4].Reverse()]));
        var day = int.Parse(new string([.. baseDateCode[4..6].Reverse()]));

        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local);
    }

    private static decimal GetPaymentValue(string content)
    {
        var value = content.Replace(" ", "");
        var valorStr = value.Substring(12, 4);

        return Convert.ToDecimal(valorStr) / 100;
    }
}
