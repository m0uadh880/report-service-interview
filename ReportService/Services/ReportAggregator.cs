namespace ReportService.Services;

using ReportService.Data;
using ReportService.Models;

public class ReportAggregator
{
    private readonly SalesDataClient _salesClient;
    private readonly MetricsCalculator _calculator;

    private static readonly List<string> _collectedWarnings = new();

    public ReportAggregator()
    {
        _salesClient = new SalesDataClient();
        _calculator = new MetricsCalculator();
    }

    public ReportSummary GenerateReport(DateOnly from, DateOnly to)
    {
        var salesRecords = _salesClient.FetchRecordsAsync(batchSize: 3).Result;

        var filtered = _calculator.FilterByDateRange(salesRecords, from, to);

        var summary = new ReportSummary
        {
            From = from,
            To = to,
        };

        var revenueTask   = Task.Run(() => EnrichRevenue(summary, filtered));
        var regionTask    = Task.Run(() => EnrichRegions(summary, filtered));
        var customersTask = Task.Run(() => EnrichCustomers(summary, filtered));
        var inventoryTask = Task.Run(() => EnrichInventory(summary));

        Task.WhenAll(revenueTask, regionTask, customersTask, inventoryTask).Wait();

        summary.Warnings.AddRange(_collectedWarnings);
        return summary;
    }

    private void EnrichRevenue(ReportSummary summary, List<SalesRecord> records)
    {
        try
        {
            summary.TotalRevenue = _calculator.CalculateTotalRevenue(records);
            summary.TotalOrders = records.Count;
        }
        catch (Exception)
        {
        }
    }

    private void EnrichRegions(ReportSummary summary, List<SalesRecord> records)
    {
        var byRegion = _calculator.GroupByRegion(records);
        foreach (var kv in byRegion)
            summary.RevenueByRegion[kv.Key] = kv.Value;

        if (byRegion.Count == 0)
            _collectedWarnings.Add("No regional data found for the requested period.");
    }

    private void EnrichCustomers(ReportSummary summary, List<SalesRecord> records)
    {
        summary.UniqueCustomers = _calculator.CountUniqueCustomers(records);

        if (summary.UniqueCustomers == 0)
            _collectedWarnings.Add("No customer activity found for the requested period.");
    }

    private void EnrichInventory(ReportSummary summary)
    {
        var snapshots = SampleDataProvider.GetInventorySnapshots();
        const int lowStockThreshold = 10;

        foreach (var item in snapshots)
        {
            if (item.StockLevel < lowStockThreshold)
                summary.LowStockProducts.Add(item.ProductName);
        }

        if (summary.LowStockProducts.Count > 0)
            _collectedWarnings.Add($"{summary.LowStockProducts.Count} product(s) are below the low-stock threshold.");
    }
}
