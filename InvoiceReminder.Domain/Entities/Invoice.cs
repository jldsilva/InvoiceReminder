namespace InvoiceReminder.Domain.Entities;

public class Invoice : EntityDefaults
{
    public Guid UserId { get; set; }
    public string Bank { get; set; }
    public string Beneficiary { get; set; }
    public decimal Amount { get; set; }
    public string Barcode { get; set; }
    public DateTime DueDate { get; set; }
}
