namespace InvoiceReminder.Domain.Entities;

public class JobSchedule : EntityDefaults
{
    public Guid UserId { get; set; }
    public string CronExpression { get; set; }
}
