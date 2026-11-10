using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels
{
    public class ProposeAppointmentViewModel
    {
        public int PatientId { get; set; }
        public int? RequestId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? RequestNumber { get; set; }

        [Required]
        [Display(Name = "Proposed date and time")]
        public DateTime ProposedAt { get; set; }

        [StringLength(1000)]
        [Display(Name = "Message for patient")]
        public string? Notes { get; set; }
    }
}
