namespace ReportService.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReportService.Services;

public class ReportAggregatorTests
{
    private static ReportAggregator CreateAggregator(int batchSize = 3, int lowStockThreshold = 10) =>
        new(
            new SalesDataClient(),
            new MetricsCalculator(),
            Options.Create(new ReportOptions { BatchSize = batchSize, LowStockThreshold = lowStockThreshold }),
            NullLogger<ReportAggregator>.Instance);

    [Fact]
    public async Task GenerateReportAsync_FirstHalf_ReturnsCorrectTotals()
    {
        var report = await CreateAggregator().GenerateReportAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 5));

        Assert.Equal(5, report.TotalOrders);
        Assert.Equal(805.50m, report.TotalRevenue);
        Assert.Equal(4, report.UniqueCustomers);
        Assert.Equal(460.00m, report.RevenueByRegion["North"]);
        Assert.Equal(85.50m, report.RevenueByRegion["South"]);
        Assert.Equal(60.00m, report.RevenueByRegion["East"]);
        Assert.Equal(200.00m, report.RevenueByRegion["West"]);
    }

    [Fact]
    public async Task GenerateReportAsync_SecondHalf_ReturnsCorrectTotals()
    {
        var report = await CreateAggregator().GenerateReportAsync(new DateOnly(2025, 1, 6), new DateOnly(2025, 1, 10));

        Assert.Equal(5, report.TotalOrders);
        Assert.Equal(1080.00m, report.TotalRevenue);
        Assert.Equal(5, report.UniqueCustomers);
    }

    // Totals must not depend on how the data is fetched.
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(50)]
    public async Task GenerateReportAsync_TotalsDoNotDependOnBatchSize(int batchSize)
    {
        var report = await CreateAggregator(batchSize).GenerateReportAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 10));

        Assert.Equal(10, report.TotalOrders);
        Assert.Equal(1885.50m, report.TotalRevenue);
        Assert.Equal(7, report.UniqueCustomers);
    }

    // Regression: warnings lived in a static list, so the second report repeated the first one's.
    [Fact]
    public async Task GenerateReportAsync_SecondReport_DoesNotRepeatFirstReportWarnings()
    {
        var aggregator = CreateAggregator();

        var first = await aggregator.GenerateReportAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 5));
        var second = await aggregator.GenerateReportAsync(new DateOnly(2025, 1, 6), new DateOnly(2025, 1, 10));

        Assert.Equal(["3 product(s) are below the low-stock threshold."], first.Warnings);
        Assert.Equal(["3 product(s) are below the low-stock threshold."], second.Warnings);
    }

    [Fact]
    public async Task GenerateReportAsync_RangeWithNoOrders_ReturnsZerosAndWarnings()
    {
        var report = await CreateAggregator().GenerateReportAsync(new DateOnly(2030, 1, 1), new DateOnly(2030, 1, 31));

        Assert.Equal(0, report.TotalOrders);
        Assert.Equal(0m, report.TotalRevenue);
        Assert.Equal(0, report.UniqueCustomers);
        Assert.Empty(report.RevenueByRegion);
        Assert.Contains("No regional data found for the requested period.", report.Warnings);
        Assert.Contains("No customer activity found for the requested period.", report.Warnings);
    }

    [Fact]
    public async Task GenerateReportAsync_UsesConfiguredLowStockThreshold()
    {
        var report = await CreateAggregator(lowStockThreshold: 5)
            .GenerateReportAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 5));

        Assert.Equal(["Widget A", "Gadget Pro"], report.LowStockProducts);
    }

    [Fact]
    public async Task GenerateReportAsync_ThresholdZero_ReportsNoLowStock()
    {
        var report = await CreateAggregator(lowStockThreshold: 0)
            .GenerateReportAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 5));

        Assert.Empty(report.LowStockProducts);
        Assert.DoesNotContain(report.Warnings, w => w.Contains("low-stock"));
    }
}
