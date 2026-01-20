using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InvoiceReminder.Application.ViewModels;

public class UserPasswordViewModel : ViewModelDefaults
{
    [JsonPropertyOrder(2)]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    public string PasswordHash { get; set; }

    [JsonIgnore]
    public string PasswordSalt { get; set; }
}
