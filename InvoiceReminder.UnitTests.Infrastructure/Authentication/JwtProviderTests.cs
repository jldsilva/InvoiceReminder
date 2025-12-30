using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Authentication.Interfaces;
using InvoiceReminder.Authentication.Jwt;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.UnitTests.Infrastructure.Authentication;

[TestClass]
public sealed class JwtProviderTests
{
    private readonly IOptions<JwtOptions> _jwtOptions;

    public JwtProviderTests()
    {
        _jwtOptions = Options.Create(new JwtOptions
        {
            Audience = "test_audience",
            Issuer = "test_issuer",
            SecretKey = "12345a56-b7c8-90d1-2e34-56fab7cde89f"
        });
    }

    [TestMethod]
    public void JwtProvider_ShouldBeAssignableToItsInterface()
    {
        // Arrange && Act
        var jwtProvider = new JwtProvider(_jwtOptions);

        // Assert
        jwtProvider.ShouldSatisfyAllConditions(() =>
        {
            _ = jwtProvider.ShouldBeAssignableTo<IJwtProvider>();
            _ = jwtProvider.ShouldNotBeNull();
            _ = jwtProvider.ShouldBeOfType<JwtProvider>();
        });
    }

    [TestMethod]
    public void JwtProvider_ShouldThrowArgumentNullExceptionWhenJwtOptionsIsNull()
    {
        // Arrange && Act
        var exception = Should.Throw<ArgumentNullException>(() => new JwtProvider(null));

        // Assert
        _ = exception.ShouldNotBeNull();
        exception.ParamName.ShouldBe("jwtOptions");
    }

    [TestMethod]
    public void JwtProvider_ShouldThrowArgumentNullExceptionWhenJwtOptionsValueIsNull()
    {
        // Arrange
        var jwtOptions = Substitute.For<IOptions<JwtOptions>>();
        _ = jwtOptions.Value.Returns((JwtOptions)null);

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => new JwtProvider(jwtOptions));

        // Assert
        _ = exception.ShouldNotBeNull();
        exception.ParamName.ShouldBe("jwtOptions.Value");
    }

    [TestMethod]
    public void JwtProvider_Generate_ValidUser_ShouldReturnToken()
    {
        // Arrange
        var jwtProvider = new JwtProvider(_jwtOptions);
        var user = new UserViewModel
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com"
        };

        // Act
        var result = jwtProvider.Generate(user);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            result.Authenticated.ShouldBe(true);
            _ = result.AuthenticationToken.ShouldNotBeNull();
            result.Expiration.ShouldNotBe(default);
        });
    }

    [TestMethod]
    public void JwtProvider_Generate_ShouldThrowArgumentNullExceptionWhenUserViewModelIsNull()
    {
        // Arrange
        var jwtProvider = new JwtProvider(_jwtOptions);

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => jwtProvider.Generate(null));

        // Assert
        _ = exception.ShouldNotBeNull();
        exception.ParamName.ShouldBe("user");
    }
}
