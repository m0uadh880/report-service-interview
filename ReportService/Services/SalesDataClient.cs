namespace ReportService.Services;

using ReportService.Models;
using ReportService.Data;

public class SalesDataClient
{
    private readonly string _baseUrl;

    public SalesDataClient(string baseUrl = "https://internal-sales-api.example.com")
    {
        _baseUrl = baseUrl;
    }

    public async Task<List<SalesRecord>> FetchRecordsAsync(int batchSize)
    {
        var allRecords = new List<SalesRecord>();
        var localData = SampleDataProvider.GetSalesRecords();
        int totalBatches = (int)Math.Ceiling(localData.Count / (double)batchSize);

        for (int i = 0; i < totalBatches; i++)
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri(_baseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);

            // Simulating async fetch with a delay (in a real system this would be an HTTP call)
            await Task.Delay(10);

            var batch = localData.Skip(i * batchSize).Take(batchSize).ToList();
            allRecords.AddRange(batch);
        }

        return allRecords;
    }
}
