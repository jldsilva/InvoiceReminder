namespace InvoiceReminder.Authentication.Jwt;

public class JwtObject
{
    public string AuthenticationToken { get; set; }
    public bool Authenticated { get; set; }
    public DateTime Expiration { get; set; }
}
