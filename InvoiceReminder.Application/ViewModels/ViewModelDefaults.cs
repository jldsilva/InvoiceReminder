using System.Text.Json.Serialization;

namespace InvoiceReminder.Application.ViewModels;

public class ViewModelDefaults
{
    [JsonPropertyOrder(1)]
    public Guid Id { get; set; }

    [JsonPropertyOrder(11)]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyOrder(12)]
    public DateTime UpdatedAt { get; set; }
}
