using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class RequestSample
    {
        [Key]
        public int SampleId { get; set; }
        public int RequestId { get; set; }

        [Required]
        public string Barcode { get; set; } = string.Empty;

        public int SampleTypeId { get; set; }
        public int? RequestTestId { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public int? ReceivedBy { get; set; }

        // === Audit trail for barcode / request-number corrections ===
        public int? EditedByTechnicianId { get; set; }
        public string? EditNote { get; set; }
        public DateTime? EditedAt { get; set; }

        // === NEW: Sample rejection (reception) ===
        public int? RejectedBy { get; set; }            // TechnicianId of the receiver who rejected
        public DateTime? RejectedAt { get; set; }
        public string? RejectionReason { get; set; }

        public SampleType? SampleType { get; set; }
        public RequestTestType? RequestTest { get; set; }
        public TestRequest? Request { get; set; }
    }
}