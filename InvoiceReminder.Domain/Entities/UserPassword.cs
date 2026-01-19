namespace InvoiceReminder.Domain.Entities;

public class UserPassword : EntityDefaults
{
    public Guid UserId { get; set; }
    public string PasswordHash { get; set; }
    public string PasswordSalt { get; set; }
}
