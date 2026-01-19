using InvoiceReminder.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InvoiceReminder.Application.ViewModels;

public class ScanEmailDefinitionViewModel : ViewModelDefaults
{
    [Required]
    [JsonPropertyOrder(2)]
    public Guid UserId { get; set; }

    [Required]
    [JsonPropertyOrder(3)]
    public InvoiceType InvoiceType { get; set; }

    [Required]
    [JsonPropertyOrder(4)]
    public string Beneficiary { get; set; }

    [Required]
    [JsonPropertyOrder(5)]
    public string Description { get; set; }

    [Required]
    [JsonPropertyOrder(6)]
    public string SenderEmailAddress { get; set; }

    [Required]
    [JsonPropertyOrder(7)]
    public string AttachmentFileName { get; set; }

    public ScanEmailDefinitionViewModel()
    {
        Id = Guid.NewGuid();
    }
}
