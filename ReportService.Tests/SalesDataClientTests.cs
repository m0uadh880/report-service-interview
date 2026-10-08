namespace ReportService.Tests;

using ReportService.Data;
using ReportService.Services;

public class SalesDataClientTests
{
    // Every batch size must return each sample record exactly once, including when the
    // last batch is partial or the batch size is larger than the dataset.
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(10)]
    [InlineData(25)]
    public async Task FetchRecordsAsync_ReturnsEveryRecordOnce(int batchSize)
    {
        var client = new SalesDataClient();
        var expected = SampleDataProvider.GetSalesRecords().Select(r => r.OrderId);

        var records = await client.FetchRecordsAsync(batchSize);

        Assert.Equal(expected, records.Select(r => r.OrderId));
    }
}
