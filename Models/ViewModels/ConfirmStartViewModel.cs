namespace NMB_HLabSys.Models.ViewModels;

public class ConfirmStartViewModel
{
    public int RequestTestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public string SampleType { get; set; } = string.Empty;

    public List<ConsumableRequirementDto> Consumables { get; set; } = new();

    // Convenience flags for the view
    public bool HasShortages => Consumables.Any(c => c.IsShort);
    public bool CanStart => !HasShortages && Consumables.Count > 0;
    public bool HasNoConsumables => Consumables.Count == 0;
}

public class ConsumableRequirementDto
{
    public int ConsumableId { get; set; }
    public string ConsumableName { get; set; } = string.Empty;
    public int RequiredQuantity { get; set; }
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public string? SupplierName { get; set; }
    public bool IsActive { get; set; } = true;

    // Derived flags
    public bool IsShort => !IsActive || QuantityOnHand < RequiredQuantity;
    public bool IsOutOfStock => !IsActive || QuantityOnHand <= 0;
    public bool IsLow => !IsOutOfStock && QuantityOnHand <= ReorderLevel;
}