using NMB_HLabSys.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels;

public class TechnicianCancelRequestViewModel
{
    [Required, Display(Name = "Request Number")]
    public string RequestNumber { get; set; } = string.Empty;

    public TestRequest? FoundRequest { get; set; }
    public bool CanCancel { get; set; }
    public string? BlockedReason { get; set; }

    [StringLength(1000, ErrorMessage = "Cancellation reason cannot exceed 1000 characters.")]
    [Display(Name = "Cancellation Reason")]
    public string? Reason { get; set; }
}
