using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class DoctorPatientConsent
    {
        [Key]
        public int ConsentId { get; set; }

        public int DoctorId { get; set; }

        public int PatientId { get; set; }

        // The parent request is retained for audit and backwards compatibility.
        public int RequestId { get; set; }

        // A consent must target one individual test or sample.
        public int? RequestTestId { get; set; }
        public int? SampleId { get; set; }

        public DateTime GrantedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Doctor? Doctor { get; set; }

        public Patient? Patient { get; set; }

        public TestRequest? Request { get; set; }

        public RequestTestType? RequestTest { get; set; }

        public RequestSample? Sample { get; set; }
    }
}