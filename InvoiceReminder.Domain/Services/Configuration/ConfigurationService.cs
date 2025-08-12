using Microsoft.Extensions.Configuration;

namespace InvoiceReminder.Domain.Services.Configuration;

public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;

    public ConfigurationService(IConfiguration configuration)
    {
        var builder = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        if (IsDevelopment())
        {
            _ = builder.AddUserSecrets<ConfigurationService>();
        }

        _configuration = builder.Build();
    }

    public string GetAppSetting(string key)
    {
        return _configuration[key];
    }

    public string GetConnectionString(string name)
    {
        return _configuration.GetConnectionString(name);
    }

    public string GetSecret(string key)
    {
        return _configuration[key];
    }

    public string GetSecret(string key, string secretName)
    {
        return _configuration[$"{key}:{secretName}"] ?? _configuration[$"{key}__{secretName}"];
    }

    public string GetSecret(string key, string secretName, string defaultValue)
    {
        return GetSecret(key, secretName) ?? defaultValue;
    }

    public T GetSection<T>(string sectionName) where T : class
    {
        return _configuration.GetSection(sectionName).Get<T>();
    }

    public T GetSection<T>(string sectionName, T defaultValue) where T : class
    {
        return GetSection<T>(sectionName) ?? defaultValue;
    }

    private static bool IsDevelopment()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    }
}
