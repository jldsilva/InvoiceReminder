using InvoiceReminder.API.AuthenticationSetup;
using InvoiceReminder.Authentication.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using System.Text;

namespace InvoiceReminder.UnitTests.API.AuthenticationSetup;

[TestClass]
public sealed class JwtBearerOptionsSetupTests
{
    private readonly IOptions<JwtOptions> _jwtOptions;

    public JwtBearerOptionsSetupTests()
    {
        _jwtOptions = Options.Create(new JwtOptions
        {
            Audience = "test_audience",
            Issuer = "test_issuer",
            SecretKey = "12345a56-b7c8-90d1-2e34-56fab7cde89f"
        });
    }

    [TestMethod]
    public void JwtBearerOptionsSetup_ValidOptions_ShouldInitializeInstance()
    {
        // Arrange && Act
        var setup = new JwtBearerOptionsSetup(_jwtOptions);

        // Assert
        _ = setup.ShouldNotBeNull();
    }

    [TestMethod]
    public void Configure_ValidOptions_ShouldSetTokenValidationParameters()
    {
        // Arrange
        var setup = new JwtBearerOptionsSetup(_jwtOptions);
        var jwtBearerOptions = new JwtBearerOptions();

        // Act
        setup.Configure(jwtBearerOptions);

        // Assert
        jwtBearerOptions.ShouldSatisfyAllConditions(() =>
        {
            _ = jwtBearerOptions.TokenValidationParameters.ShouldNotBeNull();
            jwtBearerOptions.TokenValidationParameters.ValidateIssuer.ShouldBe(true);
            jwtBearerOptions.TokenValidationParameters.ValidateAudience.ShouldBe(true);
            jwtBearerOptions.TokenValidationParameters.ValidateLifetime.ShouldBe(true);
            jwtBearerOptions.TokenValidationParameters.ValidateIssuerSigningKey.ShouldBe(true);
            _jwtOptions.Value.Issuer.ShouldBeEquivalentTo(jwtBearerOptions.TokenValidationParameters.ValidIssuer);
            _jwtOptions.Value.Audience.ShouldBeEquivalentTo(jwtBearerOptions.TokenValidationParameters.ValidAudience);
            _jwtOptions.Value.SecretKey.ShouldBeEquivalentTo(Encoding.UTF8.GetString(((SymmetricSecurityKey)
                jwtBearerOptions.TokenValidationParameters.IssuerSigningKey).Key)
            );
        });
    }

    [TestMethod]
    public void Configure_WithName_ShouldCallConfigureMethod()
    {
        // Arrange
        var setup = new JwtBearerOptionsSetup(_jwtOptions);
        var jwtBearerOptions = new JwtBearerOptions();

        // Act
        setup.Configure("TestScheme", jwtBearerOptions);

        // Assert
        jwtBearerOptions.ShouldSatisfyAllConditions(() =>
        {
            _ = jwtBearerOptions.ShouldNotBeNull();
            _ = jwtBearerOptions.TokenValidationParameters.ShouldNotBeNull();
            _jwtOptions.Value.Issuer.ShouldBeEquivalentTo(jwtBearerOptions.TokenValidationParameters.ValidIssuer);
        });
    }

    [TestMethod]
    public void Configure_NullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var setup = new JwtBearerOptionsSetup(_jwtOptions);
        JwtBearerOptions jwtBearerOptions = null;

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => setup.Configure(jwtBearerOptions));

        // Assert
        _ = exception.ShouldNotBeNull();
        exception.ParamName.ShouldBe("options");
    }
}
