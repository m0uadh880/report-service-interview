namespace ReportService.Models;

public class ReportSummary
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int UniqueCustomers { get; set; }
    public Dictionary<string, decimal> RevenueByRegion { get; set; } = new();
    public List<string> LowStockProducts { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
