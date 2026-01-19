namespace InvoiceReminder.Authentication.Abstractions;

public record UserClaims
{
    public Guid Id { get; init; }
    public string Email { get; init; }
}
