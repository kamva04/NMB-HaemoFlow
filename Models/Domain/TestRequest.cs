using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class TestRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        public string RequestNumber { get; set; } = string.Empty;

        public int PatientId { get; set; }
        public int DoctorId { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public string Urgency { get; set; } = "Routine";

        public string? ClinicalNotes { get; set; }

        public string Status { get; set; } = "Submitted";

        // Navigation properties
        public Patient? Patient { get; set; }
        public Doctor? Doctor { get; set; }

        public ICollection<RequestSample> RequestSamples { get; set; } = new List<RequestSample>();
        public ICollection<RequestTestType> RequestTestTypes { get; set; } = new List<RequestTestType>();
    }
}