using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class RequestCancellation
    {
         [Key]
        public int CancellationId { get; set; }
        public int RequestId { get; set; }
        public int CancelledBy { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CancelledAt { get; set; } = DateTime.UtcNow;
        public TestRequest? Request { get; set; }
    }
}
