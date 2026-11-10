namespace NMB_HLabSys.Models.ViewModels;

public class UsageHistoryViewModel
{
    public List<UsageHistoryRow> Rows { get; set; } = new();
    public int TotalTests => Rows.Select(r => r.RequestTestId).Distinct().Count();
    public int TotalUnitsConsumed => Rows.Sum(r => r.QuantityUsed);
}

public class UsageHistoryRow
{
    public int RequestTestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string TestTypeName { get; set; } = string.Empty;
    public DateTime UsedAt { get; set; }
    public string ConsumableName { get; set; } = string.Empty;
    public string ConsumableCode { get; set; } = string.Empty;
    public int QuantityUsed { get; set; }
}