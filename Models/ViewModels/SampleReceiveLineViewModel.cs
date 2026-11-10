namespace NMB_HLabSys.Models.ViewModels
{
    public class SampleReceiveLineViewModel
    {
        public int SampleId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string? SampleTypeName { get; set; }
        public bool IsReceived { get; set; }
        public bool IsVerified { get; set; }
    }
}
