namespace InvoiceReminder.Domain.Entities;

public class EmailAuthToken : EntityDefaults
{
    public Guid UserId { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string NonceValue { get; set; }
    public string TokenProvider { get; set; }
    public bool IsStale => AccessTokenExpiry < DateTime.UtcNow;
    public DateTime AccessTokenExpiry { get; set; }
}
