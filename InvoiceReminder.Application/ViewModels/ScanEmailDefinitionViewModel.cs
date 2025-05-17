using InvoiceReminder.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace InvoiceReminder.Application.ViewModels;
public class ScanEmailDefinitionViewModel : ViewModelDefaults
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public InvoiceType InvoiceType { get; set; }

    [Required]
    public string Beneficiary { get; set; }

    [Required]
    public string Description { get; set; }

    [Required]
    public string SenderEmailAddress { get; set; }

    [Required]
    public string AttachmentFileName { get; set; }

    public ScanEmailDefinitionViewModel()
    {
        Id = Guid.NewGuid();
    }
}
