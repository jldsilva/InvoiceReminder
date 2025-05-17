using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Authentication.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InvoiceReminder.Authentication.Jwt;

public class JwtProvider : IJwtProvider
{
    private readonly JwtOptions _jwtOptions;

    public JwtProvider(IOptions<JwtOptions> jwtOptions)
    {
        ArgumentNullException.ThrowIfNull(jwtOptions);
        ArgumentNullException.ThrowIfNull(jwtOptions.Value);

        _jwtOptions = jwtOptions.Value;
    }

    public JwtObject Generate(UserViewModel user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var claims = new Claim[]
        {
                new (JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new (JwtRegisteredClaimNames.Email, user.Email)
        };

        var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);
        var tokenHandler = new JwtSecurityTokenHandler();

        var token = new JwtSecurityToken
        (
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
            null,
            DateTime.UtcNow.AddHours(1),
            credentials
        );

        return new JwtObject
        {
            AuthenticationToken = tokenHandler.WriteToken(token),
            Authenticated = true,
            Expiration = token.ValidTo
        };
    }
}
