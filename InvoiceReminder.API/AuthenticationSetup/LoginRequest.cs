namespace InvoiceReminder.API.AuthenticationSetup;

public record LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}
