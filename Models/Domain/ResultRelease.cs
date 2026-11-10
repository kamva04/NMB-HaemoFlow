using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class ResultRelease
    {
        [Key]
        public int ReleaseId { get; set; }
        public int RequestId { get; set; }
        public int DoctorId { get; set; }
        public DateTime ReleasedAt { get; set; } = DateTime.UtcNow;
        public string? DoctorNote { get; set; }
        public string NotifyType { get; set; } = "Email";
    }
}
