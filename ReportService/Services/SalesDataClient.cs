namespace ReportService.Services;

using ReportService.Models;
using ReportService.Data;

public class SalesDataClient
{
    // FIX (socket exhaustion): a new HttpClient was created and disposed for every batch.
    // Bug: one HttpClient per request inside the loop ('using var client = new HttpClient()').
    // Cause: disposing an HttpClient closes its connection, which stays in TIME_WAIT for a
    //        while; under load the machine runs out of sockets and requests start failing.
    // Fix: one HttpClient per SalesDataClient, reused for every batch, so connections are
    //      pooled. (Injecting IHttpClientFactory would be the next step in a DI setup.)
    private readonly HttpClient _httpClient;

    public SalesDataClient(string baseUrl = "https://internal-sales-api.example.com")
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(5),
        };
    }

    public async Task<List<SalesRecord>> FetchRecordsAsync(int batchSize)
    {
        var allRecords = new List<SalesRecord>();
        var localData = SampleDataProvider.GetSalesRecords();
        int totalBatches = (int)Math.Ceiling(localData.Count / (double)batchSize);

        for (int i = 0; i < totalBatches; i++)
        {
            // Simulating async fetch with a delay (in a real system this would be an HTTP call)
            await Task.Delay(10);

            var batch = localData.Skip(i * batchSize).Take(batchSize).ToList();
            allRecords.AddRange(batch);
        }

        return allRecords;
    }
}
