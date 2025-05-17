namespace InvoiceReminder.Domain.Services.Configuration;

public interface IConfigurationService
{
    string GetAppSetting(string key);
    string GetConnectionString(string name);
    string GetSecret(string key);
    string GetSecret(string key, string secretName);
    string GetSecret(string key, string secretName, string defaultValue);
    T GetSection<T>(string sectionName) where T : class;
    T GetSection<T>(string sectionName, T defaultValue) where T : class;
}
