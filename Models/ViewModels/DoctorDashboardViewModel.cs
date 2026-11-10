namespace NMB_HLabSys.Models.ViewModels
{
    public class DoctorDashboardViewModel
    {
        public string DoctorDisplayName { get; set; } = string.Empty;
        public int PatientCount { get; set; }
        public int TestRequestCount { get; set; }
        public int ResultsCount { get; set; }
        public int CriticalAlertsCount { get; set; }
        public int PatientTrend { get; set; }
        public int TestRequestTrend { get; set; }
        public int ResultsTrend { get; set; }
        public int AlertsTrend { get; set; }
        public int ActivePatients { get; set; }
        public int PendingResults { get; set; }
        public List<DoctorDashboardRequestRow> RecentRequests { get; set; } = new();
        public List<DoctorDashboardAlertRow> CriticalAlerts { get; set; } = new();
    }

    public class DoctorDashboardRequestRow
    {
        public int RequestId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string TestTypesSummary { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
    }

    public class DoctorDashboardAlertRow
    {
        public int RequestId { get; set; }
        public int RequestTestId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public decimal? ResultValue { get; set; }
        public string? Units { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
