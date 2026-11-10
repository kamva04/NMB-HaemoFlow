using NMB_HLabSys.Models.Domain;

namespace NMB_HLabSys.Models.ViewModels
{
    public class PatientMedicalHistoryViewModel
    {
        public List<PatientCondition> Conditions { get; set; } = new();
        public List<PatientAllergy> Allergies { get; set; } = new();
        public List<PatientMedication> Medications { get; set; } = new();
    }

    public class PatientMedicalHistoryEditViewModel
    {
        public string EntryType { get; set; } = string.Empty;
        public int? CurrentItemId { get; set; }
        public int SelectedItemId { get; set; }
        public string? CurrentName { get; set; }
    }
}