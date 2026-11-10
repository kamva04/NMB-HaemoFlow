using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels;

public class CaptureResultViewModel
{
    public int RequestTestId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string? Units { get; set; }
    public decimal? NormalRangeMin { get; set; }
    public decimal? NormalRangeMax { get; set; }
    public string? ClinicalNotes { get; set; }
    public string? RequestNumber { get; set; }
    public string? PatientName { get; set; }
    public string Urgency { get; set; } = string.Empty;
    public string? Status { get; set; }

    [Required(ErrorMessage = "Result value is required.")]
    [Range(typeof(decimal), "-999999999", "999999999", ErrorMessage = "Enter a valid result value.")]
    [Display(Name = "Result Value")]
    public decimal? ResultValue { get; set; }

    [StringLength(2000)]
    [Display(Name = "Notes")]
    public string? ResultNotes { get; set; }

    public bool IsResubmission { get; set; }
}
