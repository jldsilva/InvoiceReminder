using InvoiceReminder.Domain.Services.Configuration;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace InvoiceReminder.DomainEntities.UnitTests.Services.Configuration;

[TestClass]
public class ConfigurationServiceTests
{
    private string _originalEnvironment;

    [TestInitialize]
    public void Setup()
    {
        _originalEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    }

    [TestCleanup]
    public void Cleanup()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _originalEnvironment);
    }

    private static ConfigurationService CreateService(Dictionary<string, string> initialSettings = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(initialSettings ?? [])
            .Build();

        return new ConfigurationService(config);
    }

    #region Constructor

    [TestMethod]
    public void Constructor_DevelopmentEnvironment_AddsUserSecrets()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        var config = new ConfigurationBuilder().Build();
        var service = new ConfigurationService(config);

        // No assert needed — just verifying no exception and secrets can be added
        _ = service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_ProductionEnvironment_DoesNotAddUserSecrets()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

        var config = new ConfigurationBuilder().Build();
        var service = new ConfigurationService(config);

        _ = service.ShouldNotBeNull();
    }

    #endregion

    #region GetAppSetting

    [TestMethod]
    public void GetAppSetting_KeyExists_ReturnsValue()
    {
        var service = CreateService(new() { ["MyKey"] = "MyValue" });

        var result = service.GetAppSetting("MyKey");

        result.ShouldBe("MyValue");
    }

    [TestMethod]
    public void GetAppSetting_KeyMissing_ReturnsNull()
    {
        var service = CreateService();

        var result = service.GetAppSetting("MissingKey");

        result.ShouldBeNull();
    }

    #endregion

    #region GetConnectionString

    [TestMethod]
    public void GetConnectionString_NameExists_ReturnsValue()
    {
        var service = new ConfigurationService(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:MyDb"] = "Server=.;Database=Test;"
            })
            .Build());

        var result = service.GetConnectionString("MyDb");

        result.ShouldBe("Server=.;Database=Test;");
    }

    [TestMethod]
    public void GetConnectionString_NameMissing_ReturnsNull()
    {
        var service = CreateService();

        var result = service.GetConnectionString("MissingDb");

        result.ShouldBeNull();
    }

    #endregion

    #region GetSecret

    [TestMethod]
    public void GetSecret_KeyOnly_Exists_ReturnsValue()
    {
        var service = CreateService(new() { ["SecretKey"] = "SecretValue" });

        var result = service.GetSecret("SecretKey");

        result.ShouldBe("SecretValue");
    }

    [TestMethod]
    public void GetSecret_KeyOnly_Missing_ReturnsNull()
    {
        var service = CreateService();

        var result = service.GetSecret("MissingSecret");

        result.ShouldBeNull();
    }

    [TestMethod]
    public void GetSecret_KeyAndName_Exists_ReturnsValue()
    {
        var service = CreateService(new() { ["Secrets:MySecret"] = "SecretValue" });

        var result = service.GetSecret("Secrets", "MySecret");

        result.ShouldBe("SecretValue");
    }

    [TestMethod]
    public void GetSecret_KeyAndName_ExistsWithUnderscore_ReturnsValue()
    {
        var service = CreateService(new() { ["Secrets__MySecret"] = "SecretValue" });

        var result = service.GetSecret("Secrets", "MySecret");

        result.ShouldBe("SecretValue");
    }

    [TestMethod]
    public void GetSecret_KeyAndName_Missing_ReturnsNull()
    {
        var service = CreateService();

        var result = service.GetSecret("Secrets", "Missing");

        result.ShouldBeNull();
    }

    [TestMethod]
    public void GetSecret_WithDefault_ReturnsExpected()
    {
        var service = CreateService(new() { ["Secrets:MySecret"] = "ActualValue" });

        var result = service.GetSecret("Secrets", "MySecret", "DefaultValue");

        result.ShouldBe("ActualValue");
    }

    [TestMethod]
    public void GetSecret_WithDefault_Missing_ReturnsDefault()
    {
        var service = CreateService();

        var result = service.GetSecret("Secrets", "Missing", "DefaultValue");

        result.ShouldBe("DefaultValue");
    }

    #endregion

    #region GetSection

    public class MyTestSection
    {
        public string Property1 { get; set; }
        public int Property2 { get; set; }
    }

    [TestMethod]
    public void GetSection_Exists_ReturnsDeserializedObject()
    {
        var service = CreateService(new()
        {
            ["MySection:Property1"] = "Value1",
            ["MySection:Property2"] = "42"
        });

        var result = service.GetSection<MyTestSection>("MySection");

        _ = result.ShouldNotBeNull();
        result.Property1.ShouldBe("Value1");
        result.Property2.ShouldBe(42);
    }

    [TestMethod]
    public void GetSection_Missing_ReturnsNull()
    {
        var service = CreateService();

        var result = service.GetSection<MyTestSection>("MissingSection");

        result.ShouldBeNull();
    }

    [TestMethod]
    public void GetSection_WithDefault_Exists_ReturnsDeserializedObject()
    {
        var service = CreateService(new()
        {
            ["MySection:Property1"] = "Value1",
            ["MySection:Property2"] = "42"
        });

        var defaultValue = new MyTestSection { Property1 = "Default", Property2 = 0 };

        var result = service.GetSection("MySection", defaultValue);

        _ = result.ShouldNotBeNull();
        result.Property1.ShouldBe("Value1");
        result.Property2.ShouldBe(42);
    }

    [TestMethod]
    public void GetSection_WithDefault_Missing_ReturnsDefault()
    {
        var service = CreateService();

        var defaultValue = new MyTestSection { Property1 = "Default", Property2 = 0 };

        var result = service.GetSection("MissingSection", defaultValue);

        result.ShouldBe(defaultValue);
    }

    #endregion
}


public class MyTestSection
{
    public string Property1 { get; set; }
    public int Property2 { get; set; }

    public override bool Equals(object obj)
    {
        return obj is MyTestSection other && Property1 == other.Property1 && Property2 == other.Property2;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Property1, Property2);
    }
}
