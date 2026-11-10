using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels;

public class EditSampleViewModel
{
    public int SampleId { get; set; }
    public int RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;

    // Current values (readonly display)
    public string CurrentBarcode { get; set; } = string.Empty;
    public string? CurrentRequestNumber { get; set; }

    // Editable
    [Required(ErrorMessage = "Barcode is required.")]
    [StringLength(60, ErrorMessage = "Barcode cannot exceed 60 characters.")]
    public string NewBarcode { get; set; } = string.Empty;

    public string? NewRequestNumber { get; set; }

    [Required(ErrorMessage = "You must explain why you are editing this record.")]
    [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
    public string EditNote { get; set; } = string.Empty;

    // Audit display (if already edited before)
    public string? LastEditedBy { get; set; }
    public DateTime? LastEditedAt { get; set; }
    public string? LastEditNote { get; set; }
}