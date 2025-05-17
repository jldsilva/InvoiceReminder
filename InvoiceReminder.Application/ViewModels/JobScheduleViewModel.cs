using System.ComponentModel.DataAnnotations;

namespace InvoiceReminder.Application.ViewModels;
public class JobScheduleViewModel : ViewModelDefaults
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string CronExpression { get; set; }

    public JobScheduleViewModel()
    {
        Id = Guid.NewGuid();
    }
}
