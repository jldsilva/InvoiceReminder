using System.ComponentModel.DataAnnotations;

namespace InvoiceReminder.Application.ViewModels;

public class InvoiceViewModel : ViewModelDefaults
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Bank { get; set; }

    public string Beneficiary { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    public string Barcode { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public InvoiceViewModel()
    {
        Id = Guid.NewGuid();
    }
}
