using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InvoiceReminder.Application.ViewModels;

public class EmailAuthTokenViewModel : ViewModelDefaults
{
    [Required]
    [JsonPropertyOrder(2)]
    public Guid UserId { get; set; }

    [Required]
    [JsonPropertyOrder(3)]
    public string AccessToken { get; set; }

    [Required]
    [JsonPropertyOrder(4)]
    public string RefreshToken { get; set; }

    [Required]
    [JsonPropertyOrder(5)]
    public DateTime AccessTokenExpiry { get; set; }

    [JsonPropertyOrder(6)]
    public bool IsStale => AccessTokenExpiry < DateTime.UtcNow;

    public EmailAuthTokenViewModel()
    {
        Id = Guid.NewGuid();
    }
}
