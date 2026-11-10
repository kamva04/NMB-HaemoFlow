using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class RequestTestType
    {
        [Key]
        public int RequestTestId { get; set; }
        public int RequestId { get; set; }
        public int TestTypeId { get; set; }
        public string Status { get; set; } = "Submitted";
        public int? TechnicianId { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public decimal? ResultValue { get; set; }
        public string? ResultNotes { get; set; }
        public bool IsAbnormal { get; set; }
        public int? VerifiedById { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? CancelReason { get; set; }

        // === NEW: Sent-back / re-verification tracking ===
        public bool RequiresReVerification { get; set; } = false;
        public string? ReVerificationNote { get; set; }
        public int? SentBackById { get; set; }
        public DateTime? SentBackAt { get; set; }

        // Navigation
        public TestRequest? Request { get; set; }
        public TestType? TestType { get; set; }
        public Technician? Technician { get; set; }
        public List<TestResultHistory> History { get; set; } = new();
    }
}