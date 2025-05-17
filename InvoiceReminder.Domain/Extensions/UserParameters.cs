using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Domain.Extensions;

public record UserParameters
{
    public Invoice Invoice { get; set; }
    public JobSchedule JobSchedule { get; set; }
    public ScanEmailDefinition ScanEmailDefinition { get; set; }
}
