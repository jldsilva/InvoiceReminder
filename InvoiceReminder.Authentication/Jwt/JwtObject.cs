namespace InvoiceReminder.Authentication.Jwt;

public class JwtObject
{
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string AuthenticationToken { get; set; }
    public bool Authenticated { get; set; }
    public long TelegramChatId { get; set; }
    public DateTime Expiration { get; set; }
}
