using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels
{
    public class ReleaseResultViewModel
    {
        public int RequestId { get; set; }

        public string RequestNumber { get; set; } = string.Empty;

        public string PatientName { get; set; } = string.Empty;

        public string? PatientEmail { get; set; }

        public int AbnormalCount { get; set; }

        public List<string> AbnormalTestNames { get; set; } = new();

        [Display(Name = "Message / Note for Patient")]
        public string? DoctorNote { get; set; }

        [Required, Display(Name = "Notification Method")]
        public string NotifyType { get; set; } = "Email";
    }
}