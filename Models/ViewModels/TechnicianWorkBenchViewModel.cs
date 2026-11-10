namespace NMB_HLabSys.Models.ViewModels;

// ── Work Bench ────────────────────────────────────────────────
public class TechnicianWorkBenchViewModel
{
    public string? StatusFilter { get; set; }
    public string? UrgencyFilter { get; set; }
    public string? RequestNumberFilter { get; set; }
    public string? SortBy { get; set; }
    public List<WorkBenchRowDto> Rows { get; set; } = new();
}

public class WorkBenchRowDto
{
    public int RequestTestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Patient { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public bool IsAbnormal { get; set; }
    public decimal? ResultValue { get; set; }
    public string? Units { get; set; }
}

// ── Ready to Start ───────────────────────────────────────────
public class TestReadyToStartViewModel
{
    public string? UrgencyFilter { get; set; }
    public string? RequestNumberFilter { get; set; }
    public List<ReadyToStartRowDto> Rows { get; set; } = new();
}

public class ReadyToStartRowDto
{
    public int RequestTestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Patient { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SampleType { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
}

// ── Test Requests ────────────────────────────────────────────
public class TestRequestsListViewModel
{
    public string? StatusFilter { get; set; }
    public string? UrgencyFilter { get; set; }
    public string? RequestNumberFilter { get; set; }
    public List<TestRequestRowDto> Rows { get; set; } = new();
}

public class TestRequestRowDto
{
    public int RequestTestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Patient { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

// ── Test Sent Back ───────────────────────────────────────────
public class TestSentBackViewModel
{
    public string? RequestNumberFilter { get; set; }
    public List<SentBackRowDto> Rows { get; set; } = new();
}

public class SentBackRowDto
{
    public int RequestTestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Patient { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public decimal? ResultValue { get; set; }
    public string? Units { get; set; }
    public bool IsAbnormal { get; set; }
    public DateTime? SentBackAt { get; set; }
    public string SentBackBy { get; set; } = string.Empty;
    public string? SentBackNote { get; set; }
}

// ── States ───────────────────────────────────────────────────
public class TechnicianStatesViewModel
{
    public int TotalAssigned { get; set; }
    public int Submitted { get; set; }
    public int InProgress { get; set; }
    public int Completed { get; set; }
    public int Verified { get; set; }
    public int ToBeReviewed { get; set; }
    public int Cancelled { get; set; }
    public int AbnormalCount { get; set; }
    public int CompletedToday { get; set; }
    public int VerifiedToday { get; set; }
    public int OverdueCount { get; set; }
    public double AverageTurnaroundMinutes { get; set; }
}