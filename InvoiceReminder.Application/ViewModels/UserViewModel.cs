using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InvoiceReminder.Application.ViewModels;

public class UserViewModel : ViewModelDefaults
{
    [JsonPropertyOrder(2)]
    public long TelegramChatId { get; set; }

    [Required]
    [JsonPropertyOrder(3)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    [JsonPropertyOrder(4)]
    public string Email { get; set; }

    [JsonIgnore]
    public virtual UserPasswordViewModel UserPassword { get; set; }

    [JsonPropertyOrder(5)]
    public virtual ICollection<InvoiceViewModel> Invoices { get; set; }

    [JsonPropertyOrder(6)]
    public virtual ICollection<JobScheduleViewModel> JobSchedules { get; set; }

    [JsonPropertyOrder(7)]
    public virtual ICollection<ScanEmailDefinitionViewModel> ScanEmailDefinitions { get; set; }

    public UserViewModel()
    {
        Id = Guid.NewGuid();
        Invoices = [];
        JobSchedules = [];
        ScanEmailDefinitions = [];
        UserPassword = new();
    }
}
