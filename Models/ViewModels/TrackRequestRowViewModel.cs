namespace NMB_HLabSys.Models.ViewModels
{
    public class TrackRequestRowViewModel
    {
        public int RequestId { get; set; }
        public int PatientId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string PatientSurname { get; set; } = string.Empty;
        public DateTime? PatientDateOfBirth { get; set; }
        public string TestName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string Urgency { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
        public string TestStatus { get; set; } = string.Empty;
    }
}
