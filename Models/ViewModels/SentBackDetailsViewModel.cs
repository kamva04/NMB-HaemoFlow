using NMB_HLabSys.Models.Domain;

namespace NMB_HLabSys.Models.ViewModels;

public class SentBackDetailsViewModel
{
    // IDs
    public int RequestTestId { get; set; }
    public int RequestId { get; set; }
    public int PatientId { get; set; }

    // Request / Test info
    public string RequestNumber { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool CanVerifyResult { get; set; }

    // Result
    public decimal? ResultValue { get; set; }
    public string? Units { get; set; }
    public decimal? NormalRangeMin { get; set; }
    public decimal? NormalRangeMax { get; set; }
    public bool IsAbnormal { get; set; }
    public string? ResultNotes { get; set; }

    // Patient snapshot (shown inline)
    public string PatientName { get; set; } = string.Empty;
    public DateTime? PatientDateOfBirth { get; set; }
    public int? PatientAge { get; set; }

    // Performing technician
    public string? PerformingTechnicianName { get; set; }
    public int? PerformingTechnicianId { get; set; }

    // Verifying technician who sent it back
    public string? SentBackByName { get; set; }
    public int? SentBackById { get; set; }
    public DateTime? SentBackAt { get; set; }
    public string? SentBackNote { get; set; }

    // Full history
    public List<TestResultHistoryRow> History { get; set; } = new();
}

public class TestResultHistoryRow
{
    public int HistoryId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public decimal? ResultValue { get; set; }
    public DateTime ActionDate { get; set; }
    public string? ActionByName { get; set; }
    public int? ActionByUserId { get; set; }
}