using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels
{
    public class EditResultViewModel
    {
        public int RequestTestId { get; set; }
        public int RequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public string? ResultValue { get; set; }
        public string? ResultNotes { get; set; }
        public bool IsAbnormal { get; set; }
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }
        public string? Units { get; set; }
    }
}