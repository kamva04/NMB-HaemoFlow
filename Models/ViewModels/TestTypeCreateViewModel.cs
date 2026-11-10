using NMB_HLabSys.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels
{
    public class TestTypeCreateViewModel
    {
        [Required, Display(Name = "Test Name")]
        public string TestName { get; set; } = string.Empty;

        [Required, Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required, Display(Name = "Sample Type")]
        public int SampleTypeId { get; set; }

        public string? Units { get; set; }

        [Display(Name = "Normal Range Min")]
        public decimal? NormalRangeMin { get; set; }

        [Display(Name = "Normal Range Max")]
        public decimal? NormalRangeMax { get; set; }

        [Required, Display(Name = "Turnaround Time (minutes)")]
        [Range(1, 10000)]
        public int TurnaroundMinutes { get; set; } = 60;

        [Display(Name = "Consumables Used")]
        public List<int> SelectedConsumableIds { get; set; } = new();
        public List<Consumable> AvailableConsumables { get; set; } = new();
        public List<TestCategory> Categories { get; set; } = new();
        public List<SampleType> SampleTypes { get; set; } = new();
    }
}
