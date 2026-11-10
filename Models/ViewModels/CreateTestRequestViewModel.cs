using NMB_HLabSys.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels
{
    public class CreateTestRequestViewModel
    {
        public int PatientId { get; set; }
        public string? PatientName { get; set; }

        [Required, Display(Name = "Urgency")]
        public string Urgency { get; set; } = "Routine";

        [Display(Name = "Clinical Notes")]
        public string? ClinicalNotes { get; set; }

        [Required, Display(Name = "Test Types")]
        public List<int> SelectedTestTypeIds { get; set; } = new();
        public List<TestType> AvailableTestTypes { get; set; } = new();

    }
}
