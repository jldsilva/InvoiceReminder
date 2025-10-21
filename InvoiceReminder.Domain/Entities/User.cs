namespace InvoiceReminder.Domain.Entities;

public class User : EntityDefaults
{
    public long TelegramChatId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public virtual ICollection<EmailAuthToken> EmailAuthTokens { get; set; }
    public virtual ICollection<Invoice> Invoices { get; set; }
    public virtual ICollection<JobSchedule> JobSchedules { get; set; }
    public virtual ICollection<ScanEmailDefinition> ScanEmailDefinitions { get; set; }
}
