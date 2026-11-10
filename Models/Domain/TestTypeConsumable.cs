namespace NMB_HLabSys.Models.Domain
{
    public class TestTypeConsumable
    {
        public int TestTypeId { get; set; }
        public int ConsumableId { get; set; }

        // === NEW: How much of this consumable is used per test ===
        public int QuantityUsed { get; set; } = 1;

        // Navigation
        public TestType? TestType { get; set; }
        public Consumable? Consumable { get; set; }
    }
}