using InvoiceReminder.Authentication.Abstractions;
using InvoiceReminder.Authentication.Jwt;

namespace InvoiceReminder.Authentication.Interfaces;

public interface IJwtProvider
{
    JwtObject Generate(UserClaims user);
}
