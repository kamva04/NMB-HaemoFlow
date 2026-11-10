namespace NMB_HLabSys.Models.ViewModels;

public class ManagerDashboardViewModel
{
    public string ManagerName { get; set; } = "Manager";
    public int ActiveTestTypes { get; set; }
    public int StockAttentionCount { get; set; }
    public int PendingOrderCount { get; set; }
    public int StaffCount { get; set; }
    public int CategoryCount { get; set; }
    public int SupplierCount { get; set; }
    public int PendingSupplyRequestCount { get; set; }
    public int OpenStockProblemCount { get; set; }
    public List<ManagerLowStockItem> LowStockItems { get; set; } = new();
}

public class ManagerLowStockItem
{
    public int ConsumableId { get; set; }
    public string ConsumableName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public string SupplierName { get; set; } = "Not assigned";
    public bool IsCritical => QuantityOnHand <= ReorderLevel;
}
