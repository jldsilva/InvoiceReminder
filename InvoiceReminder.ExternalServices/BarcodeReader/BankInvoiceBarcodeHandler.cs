using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Enums;
using System.Globalization;
using System.Text.RegularExpressions;

namespace InvoiceReminder.ExternalServices.BarcodeReader;

public class BankInvoiceBarcodeHandler : IInvoiceBarcodeHandler
{
    private readonly Dictionary<int, string> knowBanks;

    public InvoiceType InvoiceType => InvoiceType.BankInvoice;

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

        var (bankId, barcode) = FilterContent(content);
        var bankCode = int.Parse(bankId[..3], CultureInfo.InvariantCulture);

        return new Invoice
        {
            Bank = knowBanks.TryGetValue(bankCode, out var bankName)
                ? $"[{bankId}] - {bankName}"
                : $"[{bankId}]",
            Beneficiary = beneficiary,
            Amount = GetPaymentValue(barcode),
            Barcode = barcode,
            DueDate = GetPaymentDueDate(barcode)
        };
    }

    private static (string, string) FilterContent(string content)
    {
        var rawPattern = @"(\d{3}-\d)\s(\d+\.\d{5})\s(\d+\.\d{6})\s(\d+\.\d{6})\s(\d)\s(\d+)";
        var rawMatch = Regex.Match(content, rawPattern);
        var rawValue = rawMatch.Value;

        var bankIdPattern = @"(\d{3}-\d)";
        var bankIdMatch = Regex.Match(rawValue, bankIdPattern);

        var barcodePattern = @"(\d+\.\d{5})\s(\d+\.\d{6})\s(\d+\.\d{6})\s(\d)\s(\d+)";
        var barcodeMatch = Regex.Match(rawValue, barcodePattern);

        return bankIdMatch.Success && barcodeMatch.Success
            ? (bankIdMatch.Value, barcodeMatch.Value)
            : throw new FormatException("Não foi possível identificar o banco e a linha digitável no mesmo trecho.");
    }

    private static DateTime GetPaymentDueDate(string content)
    {
        var value = content.Replace(".", "").Replace(" ", "");
        var expirationFactor = value.Substring(33, 4);
        var baseDate = new DateTime(2022, 5, 29, 0, 0, 0, DateTimeKind.Utc);

        return baseDate.AddDays(int.Parse(expirationFactor));
    }

    private static decimal GetPaymentValue(string content)
    {
        var value = content.Replace(".", "").Replace(" ", "");
        var valorStr = value.Substring(37, 10);

        return Convert.ToDecimal(valorStr, CultureInfo.InvariantCulture) / 100;
    }
}
