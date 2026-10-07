namespace ReportService.Models;

public class SalesRecord
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTime OrderDate { get; set; }
    public string Region { get; set; } = string.Empty;
}
