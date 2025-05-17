using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Authentication.Jwt;

namespace InvoiceReminder.Authentication.Interfaces;

public interface IJwtProvider
{
    JwtObject Generate(UserViewModel user);
}
