using System;

namespace NMB_HLabSys.Models.ViewModels
{
    public class DoctorAlertViewModel
    {
        public int RequestId { get; set; }
        public int RequestTestId { get; set; }

        public string RequestNumber { get; set; } = string.Empty;

        public string PatientName { get; set; } = string.Empty;

        public string TestName { get; set; } = string.Empty;

        public decimal? ResultValue { get; set; }

        public string? Units { get; set; }

        public decimal? NormalRangeMin { get; set; }

        public decimal? NormalRangeMax { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}