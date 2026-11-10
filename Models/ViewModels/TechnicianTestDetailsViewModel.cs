using NMB_HLabSys.Models.Domain;

namespace NMB_HLabSys.Models.ViewModels;

public class TechnicianTestDetailsViewModel
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int RequestTestId { get; set; }
    public int RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string SampleType { get; set; } = string.Empty;
    public string? Units { get; set; }
    public decimal? NormalRangeMin { get; set; }
    public decimal? NormalRangeMax { get; set; }
    public int TurnaroundMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool CanVerifyResult { get; set; }
    public string Urgency { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public string? ClinicalNotes { get; set; }

    public string PatientName { get; set; } = string.Empty;
    public DateTime PatientDateOfBirth { get; set; }
    public string PatientAge { get; set; } = string.Empty;

    public List<MedicalCondition> Conditions { get; set; } = new();
    public List<Allergy> Allergies { get; set; } = new();
    public List<Medication> Medications { get; set; } = new();
    public List<RequestSampleDetailsViewModel> Samples { get; set; } = new();
}

public class RequestSampleDetailsViewModel
{
    public int SampleId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string SampleType { get; set; } = string.Empty;
    public DateTime? ReceivedAt { get; set; }
    public string? ReceivedBy { get; set; }

    // <-- NEW: needed for the technician details button
    public int? ReceivedById { get; set; }
}