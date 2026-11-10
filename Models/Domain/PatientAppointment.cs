using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class PatientAppointment
    {
        [Key]
        public int AppointmentId { get; set; }

        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int? RequestId { get; set; }

        [Required]
        public DateTime ProposedAt { get; set; }

        public string? Notes { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Patient? Patient { get; set; }
        public Doctor? Doctor { get; set; }
        public TestRequest? Request { get; set; }
    }
}
