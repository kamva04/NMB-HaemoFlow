namespace NMB_HLabSys.Models.ViewModels;

public class ConsumableManagementViewModel
{
    // Summary
    public int TotalCount { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int RemovedCount { get; set; }

    // Filters
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; } // All | OK | Low | Out | Removed

    // Rows
    public List<ConsumableRowViewModel> Consumables { get; set; } = new();
}

public class ConsumableRowViewModel
{
    public int ConsumableId { get; set; }
    public string ConsumableName { get; set; } = string.Empty;
    public int ReorderLevel { get; set; }
    public int QuantityOnHand { get; set; }
    public string? SupplierName { get; set; }
    public bool IsActive { get; set; }
    public string? RemovalReason { get; set; }
    public DateTime? RemovedAt { get; set; }

    // Computed status label
    public string Status
    {
        get
        {
            if (!IsActive) return "Removed";
            if (QuantityOnHand <= 0) return "Out";
            if (QuantityOnHand <= ReorderLevel) return "Low";
            return "OK";
        }
    }
}