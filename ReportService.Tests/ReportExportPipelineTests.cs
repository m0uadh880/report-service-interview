namespace ReportService.Tests;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ReportService.Export;
using ReportService.Models;

public class ReportExportPipelineTests
{
    private static readonly ReportSummary Summary = new() { From = new DateOnly(2025, 1, 1), To = new DateOnly(2025, 1, 5) };

    private readonly FakeLogger<ReportExportPipeline> _logger = new();

    private ReportExportPipeline CreatePipeline(params IReportExporter[] exporters) => new(exporters, _logger);

    [Fact]
    public async Task ExportAsync_OneExporterThrows_OthersStillRunAndErrorIsLogged()
    {
        var failure = new IOException("disk full");
        var recorder = new RecordingExporter("csv");
        var pipeline = CreatePipeline(new ThrowingExporter("json", failure), recorder);

        await pipeline.ExportAsync(Summary, ["json", "csv"]);

        Assert.Equal(1, recorder.Calls);
        var error = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Error);
        Assert.Same(failure, error.Exception);
    }

    // An exporter's own timeout is a failure of that format, not a request to stop everything.
    [Fact]
    public async Task ExportAsync_ExporterCancelsItselfWithoutCallerCancelling_OthersStillRun()
    {
        var recorder = new RecordingExporter("csv");
        var pipeline = CreatePipeline(new ThrowingExporter("json", new OperationCanceledException()), recorder);

        await pipeline.ExportAsync(Summary, ["json", "csv"]);

        Assert.Equal(1, recorder.Calls);
        Assert.Contains(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Error);
    }

    [Fact]
    public async Task ExportAsync_CallerCancels_CancellationPropagates()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var recorder = new RecordingExporter("csv");
        var pipeline = CreatePipeline(new ThrowingExporter("json", new OperationCanceledException(cts.Token)), recorder);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pipeline.ExportAsync(Summary, ["json", "csv"], cts.Token));
        Assert.Equal(0, recorder.Calls);
    }

    [Fact]
    public async Task ExportAsync_UnknownFormat_IsLoggedAndKnownFormatsStillRun()
    {
        var recorder = new RecordingExporter("json");
        var pipeline = CreatePipeline(recorder);

        await pipeline.ExportAsync(Summary, ["xml", "json"]);

        Assert.Equal(1, recorder.Calls);
        var warning = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning);
        Assert.Contains("xml", warning.Message);
    }

    [Fact]
    public async Task ExportAsync_SameFormatInDifferentCase_ExportsOnce()
    {
        var recorder = new RecordingExporter("json");
        var pipeline = CreatePipeline(recorder);

        await pipeline.ExportAsync(Summary, ["json", "JSON", "Json"]);

        Assert.Equal(1, recorder.Calls);
    }

    [Fact]
    public void Constructor_TwoExportersForSameFormat_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CreatePipeline(new RecordingExporter("json"), new RecordingExporter("JSON")));
    }

    private sealed class RecordingExporter(string format) : IReportExporter
    {
        public string Format => format;
        public int Calls { get; private set; }

        public Task ExportAsync(ReportSummary summary, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingExporter(string format, Exception exception) : IReportExporter
    {
        public string Format => format;

        public Task ExportAsync(ReportSummary summary, CancellationToken cancellationToken = default) =>
            Task.FromException(exception);
    }
}
