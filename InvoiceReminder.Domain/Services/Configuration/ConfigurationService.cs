using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceReminder.Domain.Services.Configuration;

public class ConfigurationService : IConfigurationService
{
    private readonly IConfigurationRoot _configuration;

    public ConfigurationService(IServiceProvider serviceProvider)
    {
        var configBuilder = serviceProvider.GetRequiredService<IConfigurationBuilder>();

        _configuration = IsDevelopment()
            ? configBuilder.AddUserSecrets<ConfigurationService>().Build()
            : configBuilder.AddJsonFile("appsettings.json").Build();
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
        return _configuration[$"{key}:{secretName}"];
    }

    public string GetSecret(string key, string secretName, string defaultValue)
    {
        return _configuration[$"{key}:{secretName}"] ?? defaultValue;
    }

    public T GetSection<T>(string sectionName) where T : class
    {
        return _configuration.GetSection(sectionName).Get<T>();
    }

    public T GetSection<T>(string sectionName, T defaultValue) where T : class
    {
        var section = _configuration.GetSection(sectionName).Get<T>();
        return section ?? defaultValue;
    }

    private static bool IsDevelopment()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    }
}
