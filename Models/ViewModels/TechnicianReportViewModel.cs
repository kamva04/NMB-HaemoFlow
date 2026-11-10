using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels;

public class TechnicianReportViewModel
{
    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "From date")]
    public DateTime FromDate { get; set; } = DateTime.Today.AddDays(-30);

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "To date")]
    public DateTime ToDate { get; set; } = DateTime.Today;

    // Optional: for showing inline after generation
    public string? TechnicianName { get; set; }
    public int? TestCount { get; set; }

    // ═══ NEW — used by Completed Tests PDF filter ═══
    [Display(Name = "Category (optional)")]
    public string? CategoryFilter { get; set; }
}