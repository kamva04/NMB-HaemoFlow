using NMB_HLabSys.Models.Domain;

namespace NMB_HLabSys.Models.ViewModels
{
    public class PatientDashboardViewModel
    {
        public Patient Patient { get; set; } = null!;
        public List<TestRequest> Requests { get; set; } = new();
    }
}
