using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class TestResultHistory
    {
        [Key]
        public int HistoryId { get; set; }
        public int RequestTestId { get; set; }
        public DateTime ActionDate { get; set; } = DateTime.UtcNow;
        public int ActionBy { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal? ResultValue { get; set; }
    }
}
