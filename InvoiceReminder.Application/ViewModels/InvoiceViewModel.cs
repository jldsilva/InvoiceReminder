using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InvoiceReminder.Application.ViewModels;

public class InvoiceViewModel : ViewModelDefaults
{
    [Required]
    [JsonPropertyOrder(2)]
    public Guid UserId { get; set; }

    [Required]
    [JsonPropertyOrder(3)]
    public string Bank { get; set; }

    [JsonPropertyOrder(4)]
    public string Beneficiary { get; set; }

    [Required]
    [JsonPropertyOrder(5)]
    public decimal Amount { get; set; }

    [Required]
    [JsonPropertyOrder(6)]
    public string Barcode { get; set; }

    [Required]
    [JsonPropertyOrder(7)]
    public DateTime DueDate { get; set; }

    public InvoiceViewModel()
    {
        Id = Guid.NewGuid();
    }
}
