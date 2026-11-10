using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class TestType
    {
        [Key]
        public int TestTypeId { get; set; }
        public int CategoryId { get; set; }

        [Required] public string TestName { get; set; } = string.Empty;
        public int SampleTypeId { get; set; }
        public string? Units { get; set; }
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }
        public int TurnaroundMinutes { get; set; } = 60;
        public bool IsActive { get; set; } = true;
        public TestCategory? Category { get; set; }
        public SampleType? SampleType { get; set; }
        public List<Consumable> Consumables { get; set; } = new();

		public List<TestTypeConsumable> TestTypeConsumables { get; set; } = new();

	}
}
