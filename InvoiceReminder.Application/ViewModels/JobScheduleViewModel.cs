using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InvoiceReminder.Application.ViewModels;

public class JobScheduleViewModel : ViewModelDefaults
{
    [Required]
    [JsonPropertyOrder(2)]
    public Guid UserId { get; set; }

    [Required]
    [JsonPropertyOrder(3)]
    public string CronExpression { get; set; }

    public JobScheduleViewModel()
    {
        Id = Guid.NewGuid();
    }
}
