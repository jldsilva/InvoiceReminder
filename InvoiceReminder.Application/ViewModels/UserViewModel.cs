using System.ComponentModel.DataAnnotations;

namespace InvoiceReminder.Application.ViewModels;

public class UserViewModel : ViewModelDefaults
{
    public long TelegramChatId { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; }

    public virtual ICollection<InvoiceViewModel> Invoices { get; set; }

    public virtual ICollection<JobScheduleViewModel> JobSchedules { get; set; }

    public virtual ICollection<ScanEmailDefinitionViewModel> ScanEmailDefinitions { get; set; }

    public UserViewModel()
    {
        Id = Guid.NewGuid();
        Invoices = [];
        JobSchedules = [];
        ScanEmailDefinitions = [];
    }
}
