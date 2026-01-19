namespace InvoiceReminder.API.AuthenticationSetup;

public record LoginRequest
{
    public string Email { get; init; }
    public string Password { get; init; }
}
