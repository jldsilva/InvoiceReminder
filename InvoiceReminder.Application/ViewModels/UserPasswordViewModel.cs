using System.Text.Json.Serialization;

namespace InvoiceReminder.Application.ViewModels;

public class UserPasswordViewModel : ViewModelDefaults
{
    [JsonPropertyOrder(2)]
    public Guid UserId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    public string PasswordHash { get; set; }

    [JsonIgnore]
    public string PasswordSalt { get; set; }
}
