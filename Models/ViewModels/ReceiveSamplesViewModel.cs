using NMB_HLabSys.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels;

public class ReceiveSamplesViewModel
{
    [Required, Display(Name = "Request Number")]
    public string RequestNumber { get; set; } = string.Empty;
    public TestRequest? FoundRequest { get; set; }
    public string TechnicianName { get; set; } = string.Empty;

    [Display(Name = "Samples to Receive")]
    public List<SampleReceiveLineViewModel> Samples { get; set; } = new();

    // NEW — barcode verifier state
    public int VerifiedBarcodeCount { get; set; }
    public string? LastVerifiedBarcode { get; set; }
}
