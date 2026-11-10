using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels;

public class VerifyResultViewModel
{
    public int RequestTestId { get; set; }
    public string? RequestNumber { get; set; }
    public string? PatientName { get; set; }
    public string? TestName { get; set; }
    public decimal? ResultValue { get; set; }
    public string? ResultNotes { get; set; }
    public bool IsAbnormal { get; set; }
    public decimal? NormalRangeMin { get; set; }
    public decimal? NormalRangeMax { get; set; }
    public string? Units { get; set; }
    public string? PerformingTechnician { get; set; }

    [Required(ErrorMessage = "Please select a verification decision.")]
    public bool? IsApproved { get; set; }

    [StringLength(1000)]
    public string? ReviewNote { get; set; }
}
