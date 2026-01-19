namespace InvoiceReminder.Authentication.Abstractions;

public record UserClaims
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
}
