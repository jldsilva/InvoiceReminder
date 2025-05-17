using InvoiceReminder.Domain.Services.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.DomainEntities.UnitTests.Services.Configuration;

[TestClass]
public class ConfigurationServiceTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationBuilder _configurationBuilder;
    private readonly IConfigurationRoot _configurationRoot;
    private readonly ConfigurationService _service;

    public ConfigurationServiceTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _configurationBuilder = Substitute.For<IConfigurationBuilder>();
        _configurationRoot = Substitute.For<IConfigurationRoot>();

        _ = _serviceProvider.GetService(typeof(IConfigurationBuilder)).Returns(_configurationBuilder);

        _ = _configurationBuilder.Add(Arg.Any<IConfigurationSource>()).Returns(_configurationBuilder);

        _ = _configurationBuilder.Build().Returns(_configurationRoot);

        _service = new ConfigurationService(_serviceProvider);
    }

    [TestMethod]
    public void Constructor_WhenEnvironmentIsDevelopment_AddsUserSecretsAndBuilds()
    {
        // Arrange
        var originalEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        try
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            var configBuilder = Substitute.For<IConfigurationBuilder>();
            var configRoot = Substitute.For<IConfigurationRoot>();

            _ = serviceProvider.GetService(typeof(IConfigurationBuilder)).Returns(configBuilder);
            _ = configBuilder.Add(Arg.Any<IConfigurationSource>()).Returns(configBuilder);
            _ = configBuilder.Build().Returns(configRoot);

            // Act
            _ = new ConfigurationService(serviceProvider);

            // Assert
            _ = serviceProvider.Received(1).GetService(typeof(IConfigurationBuilder));

            _ = configBuilder.Received(1).Add(Arg.Is<IConfigurationSource>(s => s is IConfigurationSource));

            _ = configBuilder.Received(1).Build();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnvironment);
        }
    }

    [TestMethod]
    public void Constructor_WhenEnvironmentIsNotDevelopment_AddsJsonFileAndBuilds()
    {
        // Arrange
        var originalEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

        try
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            var configBuilder = Substitute.For<IConfigurationBuilder>();
            var configRoot = Substitute.For<IConfigurationRoot>();

            _ = serviceProvider.GetService(typeof(IConfigurationBuilder)).Returns(configBuilder);
            _ = configBuilder.Add(Arg.Any<IConfigurationSource>()).Returns(configBuilder);
            _ = configBuilder.Build().Returns(configRoot);

            // Act
            _ = new ConfigurationService(serviceProvider);

            // Assert
            _ = serviceProvider.Received(1).GetService(typeof(IConfigurationBuilder));

            _ = configBuilder.Received(1).Add(Arg.Is<IConfigurationSource>(s =>
                s is JsonConfigurationSource && ((JsonConfigurationSource)s).Path == "appsettings.json"));

            _ = configBuilder.Received(1).Build();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnvironment);
        }
    }

    [TestMethod]
    public void GetAppSetting_WhenKeyExists_ReturnsValue()
    {
        // Arrange
        var key = "TestKey";
        var expectedValue = "TestValue";

        _ = _configurationRoot[key].Returns(expectedValue);

        // Act
        var actualValue = _service.GetAppSetting(key);

        // Assert
        actualValue.ShouldBe(expectedValue);
    }

    [TestMethod]
    public void GetAppSetting_WhenKeyDoesNotExist_ReturnsNull()
    {
        // Arrange
        var key = "NonExistentKey";

        _ = _configurationRoot[key].Returns((string)null);

        // Act
        var actualValue = _service.GetAppSetting(key);

        // Assert
        actualValue.ShouldBeNull();
    }

    [TestMethod]
    public void GetConnectionString_WhenNameExists_ReturnsConnectionString()
    {
        // Arrange
        var name = "TestConnection";
        var expectedConnectionString = "Server=localhost;Database=TestDB;";

        _ = _configurationRoot.GetConnectionString(name).Returns(expectedConnectionString);

        // Act
        var actualConnectionString = _service.GetConnectionString(name);

        // Assert
        actualConnectionString.ShouldBe(expectedConnectionString);
    }

    [TestMethod]
    public void GetConnectionString_WhenNameDoesNotExist_ReturnsNull()
    {
        // Arrange
        var name = "NonExistentConnection";

        _ = _configurationRoot.GetConnectionString(name).Returns((string)null);

        // Act
        var actualConnectionString = _service.GetConnectionString(name);

        // Assert
        actualConnectionString.ShouldBeNull();
    }

    [TestMethod]
    public void GetSecret_WithKeyOnly_WhenKeyExists_ReturnsValue()
    {
        // Arrange
        var key = "TestSecretKey";
        var expectedValue = "SuperSecretValue";

        _ = _configurationRoot[key].Returns(expectedValue);

        // Act
        var actualValue = _service.GetSecret(key);

        // Assert
        actualValue.ShouldBe(expectedValue);
    }

    [TestMethod]
    public void GetSecret_WithKeyOnly_WhenKeyDoesNotExist_ReturnsNull()
    {
        // Arrange
        var key = "NonExistentSecretKey";

        _ = _configurationRoot[key].Returns((string)null);

        // Act
        var actualValue = _service.GetSecret(key);

        // Assert
        actualValue.ShouldBeNull();
    }

    [TestMethod]
    public void GetSecret_WithKeyAndSecretName_WhenKeyExists_ReturnsValue()
    {
        // Arrange
        var key = "Secrets";
        var secretName = "MySecret";
        var fullKey = $"{key}:{secretName}";
        var expectedValue = "AnotherSecretValue";

        _ = _configurationRoot[fullKey].Returns(expectedValue);

        // Act
        var actualValue = _service.GetSecret(key, secretName);

        // Assert
        actualValue.ShouldBe(expectedValue);
    }

    [TestMethod]
    public void GetSecret_WithKeyAndSecretName_WhenKeyDoesNotExist_ReturnsNull()
    {
        // Arrange
        var key = "Secrets";
        var secretName = "NonExistentSecret";
        var fullKey = $"{key}:{secretName}";

        _ = _configurationRoot[fullKey].Returns((string)null);

        // Act
        var actualValue = _service.GetSecret(key, secretName);

        // Assert
        actualValue.ShouldBeNull();
    }

    [TestMethod]
    public void GetSecret_WithKeySecretNameAndDefault_WhenKeyExists_ReturnsValue()
    {
        // Arrange
        var key = "Secrets";
        var secretName = "MySpecificSecret";
        var defaultValue = "DefaultSecret";
        var fullKey = $"{key}:{secretName}";
        var expectedValue = "SpecificSecretValue";

        _ = _configurationRoot[fullKey].Returns(expectedValue);

        // Act
        var actualValue = _service.GetSecret(key, secretName, defaultValue);

        // Assert
        actualValue.ShouldBe(expectedValue);
    }

    [TestMethod]
    public void GetSecret_WithKeySecretNameAndDefault_WhenKeyDoesNotExist_ReturnsDefaultValue()
    {
        // Arrange
        var key = "Secrets";
        var secretName = "YetAnotherNonExistentSecret";
        var defaultValue = "MyDefaultValue";
        var fullKey = $"{key}:{secretName}";

        _ = _configurationRoot[fullKey].Returns((string)null);

        // Act
        var actualValue = _service.GetSecret(key, secretName, defaultValue);

        // Assert
        actualValue.ShouldBe(defaultValue);
    }

    [TestMethod]
    public void GetSection_WhenSectionExists_ReturnsDeserializedObject()
    {
        // Arrange
        var sectionName = "TestSection";
        var expectedSection = new MyTestSection { Property1 = "Val1", Property2 = 123 };

        var inMemoryConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {$"{sectionName}:Property1", expectedSection.Property1},
                {$"{sectionName}:Property2", expectedSection.Property2.ToString()},
            })
            .Build();

        _ = _configurationRoot.GetSection(sectionName).Returns(inMemoryConfig.GetSection(sectionName));

        // Act
        var actualSection = _service.GetSection<MyTestSection>(sectionName);

        // Assert
        _ = actualSection.ShouldNotBeNull();
        actualSection.Property1.ShouldBe(expectedSection.Property1);
        actualSection.Property2.ShouldBe(expectedSection.Property2);
    }

    [TestMethod]
    public void GetSection_WhenSectionDoesNotExist_ReturnsNull()
    {
        // Arrange
        var sectionName = "NonExistentSection";
        var inMemoryConfig = new ConfigurationBuilder().Build();

        _ = _configurationRoot.GetSection(sectionName).Returns(inMemoryConfig.GetSection(sectionName));

        // Act
        var actualSection = _service.GetSection<MyTestSection>(sectionName);

        // Assert
        actualSection.ShouldBeNull();
    }

    [TestMethod]
    public void GetSection_WithDefault_WhenSectionExists_ReturnsDeserializedObject()
    {
        // Arrange
        var sectionName = "ExistingSectionForDefaultTest";
        var expectedSection = new MyTestSection { Property1 = "Data", Property2 = 456 };
        var defaultValue = new MyTestSection { Property1 = "Default", Property2 = 0 };

        var inMemoryConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {$"{sectionName}:Property1", expectedSection.Property1},
                {$"{sectionName}:Property2", expectedSection.Property2.ToString()},
            })
            .Build();

        _ = _configurationRoot.GetSection(sectionName).Returns(inMemoryConfig.GetSection(sectionName));

        // Act
        var actualSection = _service.GetSection(sectionName, defaultValue);

        // Assert
        _ = actualSection.ShouldNotBeNull();
        actualSection.Property1.ShouldBe(expectedSection.Property1);
        actualSection.Property2.ShouldBe(expectedSection.Property2);
    }

    [TestMethod]
    public void GetSection_WithDefault_WhenSectionDoesNotExist_ReturnsDefaultValue()
    {
        // Arrange
        var sectionName = "NonExistentSectionForDefaultTest";
        var defaultValue = new MyTestSection { Property1 = "DefaultData", Property2 = 789 };

        var inMemoryConfig = new ConfigurationBuilder().Build();
        _ = _configurationRoot.GetSection(sectionName).Returns(inMemoryConfig.GetSection(sectionName));

        // Act
        var actualSection = _service.GetSection(sectionName, defaultValue);

        // Assert
        _ = actualSection.ShouldNotBeNull();
        actualSection.ShouldBe(defaultValue);
    }
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
