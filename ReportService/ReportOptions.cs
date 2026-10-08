namespace ReportService;

public class ReportOptions
{
    public const string SectionName = "Report";

    public int BatchSize { get; set; } = 3;
    public int LowStockThreshold { get; set; } = 10;
}
