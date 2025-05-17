using InvoiceReminder.API.AuthenticationSetup;
using InvoiceReminder.Authentication.Jwt;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace InvoiceReminder.API.UnitTests.AuthenticationSetup;

[TestClass]
public sealed class JwtOptionsSetupTests
{
    private readonly IDictionary<string, string> _inMemorySettings;
    private readonly string _secretKey;

    public JwtOptionsSetupTests()
    {
        _secretKey = "12345a56-b7c8-90d1-2e34-56fab7cde89f";

        _inMemorySettings = new Dictionary<string, string>
        {
            {"JwtOptions:Audience", "test_audience"},
            {"JwtOptions:Issuer", "test_issuer"},
            {"JwtOptions:SecretKey", _secretKey}

        };
    }

    [TestMethod]
    public void Configure_ValidConfiguration_ShouldBindJwtOptions()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(_inMemorySettings)
            .Build();

        var options = new JwtOptions();
        var jwtOptionsSetup = new JwtOptionsSetup(configuration);

        // Act
        jwtOptionsSetup.Configure(options);

        // Assert
        options.ShouldSatisfyAllConditions(() =>
        {
            _ = options.ShouldNotBeNull();
            options.Audience.ShouldBeEquivalentTo("test_audience");
            options.Issuer.ShouldBeEquivalentTo("test_issuer");
            options.SecretKey.ShouldBeEquivalentTo(_secretKey);
        });
    }

    [TestMethod]
    public void Configure_MissingValues_ShouldBindPartialJwtOptions()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string>
        {
            {"JwtOptions:SecretKey", _secretKey}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var options = new JwtOptions();
        var jwtOptionsSetup = new JwtOptionsSetup(configuration);

        // Act
        jwtOptionsSetup.Configure(options);

        // Assert
        options.ShouldSatisfyAllConditions(() =>
        {
            _ = options.ShouldNotBeNull();
            options.Audience.ShouldBeNull();
            options.Issuer.ShouldBeNull();
            options.SecretKey.ShouldBeEquivalentTo(_secretKey);
        });
    }

    [TestMethod]
    public void Configure_EmptyConfiguration_ShouldBindDefaultJwtOptions()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var options = new JwtOptions();
        var jwtOptionsSetup = new JwtOptionsSetup(configuration);

        // Act
        jwtOptionsSetup.Configure(options);

        // Assert
        options.ShouldSatisfyAllConditions(() =>
        {
            _ = options.ShouldNotBeNull();
            options.Audience.ShouldBeNull();
            options.Issuer.ShouldBeNull();
            options.SecretKey.ShouldBeNull();
        });
    }

    [TestMethod]
    public void Configure_NullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var jwtOptionsSetup = new JwtOptionsSetup(null);

        // Act && Assert
        _ = Should.Throw<ArgumentNullException>(() => jwtOptionsSetup.Configure(null));
    }

    [TestMethod]
    public void Configure_NonExistingSection_ShouldNotThrowException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var jwtOptionsSetup = new JwtOptionsSetup(configuration);
        var options = new JwtOptions();

        // Act
        jwtOptionsSetup.Configure(options);

        // Assert
        options.ShouldSatisfyAllConditions(() =>
        {
            options.Audience.ShouldBeNull();
            options.Issuer.ShouldBeNull();
            options.SecretKey.ShouldBeNull();
        });
    }
}
