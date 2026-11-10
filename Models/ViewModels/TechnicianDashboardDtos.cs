namespace NMB_HLabSys.Models.ViewModels;

public class RecentSampleDto
{
    public int SampleId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string SampleType { get; set; } = string.Empty;
    public string RequestNumber { get; set; } = string.Empty;
    public string Patient { get; set; } = string.Empty;
    public DateTime? ReceivedAt { get; set; }
}

public class PendingSampleDto
{
    public int RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Patient { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public string Urgency { get; set; } = string.Empty;
    public int SampleCount { get; set; }
}

public class InProgressTestDto
{
    public int RequestTestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public string Patient { get; set; } = string.Empty;
}

public class ReadyToStartDto
{
    public int RequestTestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public string Patient { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
}

public class AwaitingVerificationDto
{
    public int RequestTestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public decimal? ResultValue { get; set; }
    public string? Units { get; set; }
    public bool IsAbnormal { get; set; }
    public string Patient { get; set; } = string.Empty;
}

public class ToReviewDto
{
    public int RequestTestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public string Patient { get; set; } = string.Empty;
}

public class VerifiedHistoryDto
{
    public int RequestTestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public decimal? ResultValue { get; set; }
    public string? Units { get; set; }
    public bool IsAbnormal { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string Patient { get; set; } = string.Empty;
}
