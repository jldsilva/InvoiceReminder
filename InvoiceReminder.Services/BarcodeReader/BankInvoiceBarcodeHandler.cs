using InvoiceReminder.Domain.Entities;
using System.Text.RegularExpressions;

namespace InvoiceReminder.ExternalServices.BarcodeReader;

public class BankInvoiceBarcodeHandler : IInvoiceBarcodeHandler
{
    private readonly Dictionary<int, string> knowBanks;

    public BankInvoiceBarcodeHandler()
    {
        knowBanks = new()
        {
            { 1, "Banco do Brasil" },
            { 237, "Bradesco" },
            { 341, "Itaú" },
            { 104, "Caixa Econômica Federal" },
            { 33, "Santander" },
            { 422, "Safra" },
            { 745, "Citibank" },
            { 208, "BTG Pactual" }
        };
    }

    public Invoice CreateInvoice(string content, string beneficiary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var barcode = FilterContent(content);
        var bankId = int.Parse(barcode.Split("\n")[0][..3]);
        var paymentCode = barcode.Split("\n")[1];

        return new Invoice
        {
            Bank = $"[{bankId}] - {knowBanks[bankId]}",
            Beneficiary = beneficiary,
            Amount = GetPaymentValue(paymentCode),
            Barcode = paymentCode,
            DueDate = GetPaymentDueDate(paymentCode)
        };
    }

    private static string FilterContent(string content)
    {
        var pattern = @"(\d{3}-\d)\s(\d+\.\d{5})\s(\d+\.\d{6})\s(\d+\.\d{6})\s(\d)\s(\d+)";

        var match = Regex.Match(content, pattern);

        return match.Groups[0].Value;
    }

    private static DateTime GetPaymentDueDate(string content)
    {
        var value = content.Replace(".", "").Replace(" ", "");
        var expirationFactor = value.Substring(33, 4);
        var baseDate = new DateTime(2022, 5, 29, 0, 0, 0, DateTimeKind.Local);

        return baseDate.AddDays(int.Parse(expirationFactor));
    }

    private static decimal GetPaymentValue(string content)
    {
        var value = content.Replace(".", "").Replace(" ", "");
        var valorStr = value.Substring(37, 10);

        return Convert.ToDecimal(valorStr) / 100;
    }
}
