using NMB_HLabSys.Models.Domain;

namespace NMB_HLabSys.Models.ViewModels;

public class TechnicianDashboardViewModel
{
    public Technician Technician { get; set; } = null!;

    public string? UrgencyFilter { get; set; }
    public int? CategoryId { get; set; }
    public string? DueFilter { get; set; }
    public string? RequestNumberFilter { get; set; }
    public string? SortBy { get; set; }

    public List<TestCategory> Categories { get; set; } = new();

    public List<RecentSampleDto> RecentSamples { get; set; } = new();
    public List<PendingSampleDto> PendingSamples { get; set; } = new();
    public List<InProgressTestDto> InProgress { get; set; } = new();
    public List<ReadyToStartDto> ReadyToStart { get; set; } = new();
    public List<AwaitingVerificationDto> AwaitingVerification { get; set; } = new();
    public List<ToReviewDto> ToReview { get; set; } = new();
    public List<VerifiedHistoryDto> VerifiedHistory { get; set; } = new();

    // Consumables snapshot (lab-wide)
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }

    // Card counts
    public int PendingSamplesCount => PendingSamples.Count;
    public int InProgressCount => InProgress.Count;
    public int ReadyToStartCount => ReadyToStart.Count;
    public int AwaitingVerificationCount => AwaitingVerification.Count;
    public int ToReviewCount => ToReview.Count;
    public int VerifiedTodayCount { get; set; }

    public int UrgentCount =>
        InProgress.Count(x => IsStat(x.Urgency)) + ReadyToStart.Count(x => IsStat(x.Urgency));

    public int OverdueCount =>
        InProgress.Count(x => x.DueAt.HasValue && x.DueAt.Value < DateTime.Now);

    // Nearing: <= 60 min remaining
    public int NearingDueCount => InProgress.Count(x =>
        x.DueAt.HasValue
        && x.DueAt.Value >= DateTime.Now
        && x.DueAt.Value <= DateTime.Now.AddMinutes(60));

    // Critical: <= 15 min remaining
    public int CriticalDueCount => InProgress.Count(x =>
        x.DueAt.HasValue
        && x.DueAt.Value >= DateTime.Now
        && x.DueAt.Value <= DateTime.Now.AddMinutes(15));

    public bool HasAnyAlert =>
        OverdueCount > 0
        || CriticalDueCount > 0
        || OutOfStockCount > 0
        || ReadyToStart.Any(x => IsStat(x.Urgency));

    private static bool IsStat(string? urgency) =>
        string.Equals(urgency, "Stat", StringComparison.OrdinalIgnoreCase);
}