using InvoiceReminder.Domain.Enums;

namespace InvoiceReminder.Domain.Entities;

public class ScanEmailDefinition : EntityDefaults
{
    public Guid UserId { get; set; }
    public InvoiceType InvoiceType { get; set; }
    public string Beneficiary { get; set; }
    public string Description { get; set; }
    public string SenderEmailAddress { get; set; }
    public string AttachmentFileName { get; set; }
}
