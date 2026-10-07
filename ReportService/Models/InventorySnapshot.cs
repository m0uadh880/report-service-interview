namespace ReportService.Models;

public class InventorySnapshot
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int StockLevel { get; set; }
    public DateTime SnapshotDate { get; set; }
}
