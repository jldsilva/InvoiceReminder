using InvoiceReminder.Authentication.Jwt;
using Microsoft.Extensions.Options;

namespace InvoiceReminder.API.AuthenticationSetup;

public class JwtOptionsSetup : IConfigureOptions<JwtOptions>
{
    private const string SectionName = "JwtOptions";
    private readonly IConfiguration _configuration;

    public JwtOptionsSetup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _configuration.GetSection(SectionName).Bind(options);
    }
}
