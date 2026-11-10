using NMB_HLabSys.Models.Domain;

namespace NMB_HLabSys.Models.ViewModels
{
    public class TestRequestDetailsViewModel
    {

        public TestRequest Request { get; set; } = null!;
        public List<RequestTestType> TestResults { get; set; } = new();
        public string? CancellationReason { get; set; }
        public bool HasAbnormal => TestResults.Exists(t => t.IsAbnormal);
        public DateTime? ReleasedAt { get; set; }
        public string? ReleaseDoctorNote { get; set; }
        public string? ReleaseNotifyType { get; set; }
    }
}
