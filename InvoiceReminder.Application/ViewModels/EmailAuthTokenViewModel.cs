using System.ComponentModel.DataAnnotations;

namespace InvoiceReminder.Application.ViewModels;
public class EmailAuthTokenViewModel : ViewModelDefaults
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string AccessToken { get; set; }

    [Required]
    public string RefreshToken { get; set; }

    [Required]
    public DateTime AccessTokenExpiry { get; set; }

    public bool IsStale => AccessTokenExpiry < DateTime.UtcNow;

    public EmailAuthTokenViewModel()
    {
        Id = Guid.NewGuid();
    }
}
