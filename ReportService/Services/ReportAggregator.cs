namespace ReportService.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportService.Data;
using ReportService.Models;

public class ReportAggregator
{
    private readonly SalesDataClient _salesClient;
    private readonly MetricsCalculator _calculator;
    private readonly ReportOptions _options;
    private readonly ILogger<ReportAggregator> _logger;

    // FIX (stale warnings): removed 'private static readonly List<string> _collectedWarnings'.
    // Bug: warnings were collected in a static list that was never cleared.
    // Cause: static state is shared by every report, so the second report repeated the
    //        first report's warnings (and the list grew for the lifetime of the process).
    // Fix: enrichers write to summary.Warnings, which belongs to one report only.

    public ReportAggregator(
        SalesDataClient salesClient,
        MetricsCalculator calculator,
        IOptions<ReportOptions> options,
        ILogger<ReportAggregator> logger)
    {
        _salesClient = salesClient;
        _calculator = calculator;
        _options = options.Value;
        _logger = logger;
    }

    // FIX (sync-over-async): was 'ReportSummary GenerateReport(...)' using .Result and .Wait().
    // Bug: the method blocked on asynchronous work.
    // Cause: each blocked call holds a thread-pool thread while doing nothing; under load the
    //        pool starves and requests time out, and under a synchronization context
    //        (UI, classic ASP.NET) the continuation can never resume, so it deadlocks.
    // Fix: the method is async and awaits the fetch, so no thread is held while waiting.
    public async Task<ReportSummary> GenerateReportAsync(DateOnly from, DateOnly to)
    {
        var salesRecords = await _salesClient.FetchRecordsAsync(_options.BatchSize);

        var filtered = _calculator.FilterByDateRange(salesRecords, from, to);

        var summary = new ReportSummary
        {
            From = from,
            To = to,
        };

        // FIX (race condition): the enrichers used to run in parallel via Task.Run.
        // Bug: four threads mutated the same summary and the same List<string> at once.
        // Cause: List<T> and Dictionary<TKey,TValue> are not thread-safe; concurrent Add can
        //        lose entries, throw, or corrupt the collection, so output varied between runs.
        // Fix: run them sequentially. They are cheap in-memory LINQ, so parallelism gained
        //      nothing; sequential execution removes the race and gives a stable warning order.
        RunEnricher(summary, "Revenue",   () => EnrichRevenue(summary, filtered));
        RunEnricher(summary, "Regions",   () => EnrichRegions(summary, filtered));
        RunEnricher(summary, "Customers", () => EnrichCustomers(summary, filtered));
        RunEnricher(summary, "Inventory", () => EnrichInventory(summary));

        return summary;
    }

    // FIX (swallowed exceptions): only EnrichRevenue had a try/catch, and its catch was empty.
    // Bug: a revenue failure was silently ignored; failures in other enrichers were unhandled.
    // Cause: the empty catch left TotalRevenue/TotalOrders at 0, so a broken report looked
    //        like a valid report with no sales, and nothing was logged.
    // Fix: every enricher runs through this wrapper. A failure is logged with the exception
    //      and added to the report as a warning, and the other sections are still produced.
    //      OperationCanceledException is not caught, so a cancelled request stays cancelled.
    private void RunEnricher(ReportSummary summary, string section, Action enrich)
    {
        try
        {
            enrich();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "{Section} section failed for report {From}..{To}", section, summary.From, summary.To);
            summary.Warnings.Add($"{section} data could not be calculated. Values shown for this section are incomplete.");
        }
    }

    private void EnrichRevenue(ReportSummary summary, List<SalesRecord> records)
    {
        summary.TotalRevenue = _calculator.CalculateTotalRevenue(records);
        summary.TotalOrders = records.Count;
    }

    private void EnrichRegions(ReportSummary summary, List<SalesRecord> records)
    {
        var byRegion = _calculator.GroupByRegion(records);
        foreach (var kv in byRegion)
            summary.RevenueByRegion[kv.Key] = kv.Value;

        if (byRegion.Count == 0)
            summary.Warnings.Add("No regional data found for the requested period.");
    }

    private void EnrichCustomers(ReportSummary summary, List<SalesRecord> records)
    {
        summary.UniqueCustomers = _calculator.CountUniqueCustomers(records);

        if (summary.UniqueCustomers == 0)
            summary.Warnings.Add("No customer activity found for the requested period.");
    }

    private void EnrichInventory(ReportSummary summary)
    {
        var snapshots = SampleDataProvider.GetInventorySnapshots();

        foreach (var item in snapshots)
        {
            if (item.StockLevel < _options.LowStockThreshold)
                summary.LowStockProducts.Add(item.ProductName);
        }

        if (summary.LowStockProducts.Count > 0)
            summary.Warnings.Add($"{summary.LowStockProducts.Count} product(s) are below the low-stock threshold.");
    }
}
