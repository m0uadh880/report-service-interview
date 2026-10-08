namespace ReportService.Tests;

using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using ReportService.Export;
using ReportService.Models;

public sealed class ExporterTests : IDisposable
{
    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), "report-export-tests", Guid.NewGuid().ToString("N"));
    private readonly HostingEnvironment _environment;
    private readonly IOptions<ExportOptions> _options = Options.Create(new ExportOptions { OutputDirectory = "out" });

    public ExporterTests()
    {
        _environment = new HostingEnvironment { ContentRootPath = _contentRoot };
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
            Directory.Delete(_contentRoot, recursive: true);
    }

    private string OutputPath(string extension) => Path.Combine(_contentRoot, "out", $"report_20250101_20250105.{extension}");

    private static ReportSummary CreateSummary() => new()
    {
        From = new DateOnly(2025, 1, 1),
        To = new DateOnly(2025, 1, 5),
        TotalRevenue = 805.5m,
        TotalOrders = 5,
        UniqueCustomers = 4,
        RevenueByRegion = { ["North"] = 460m, ["South"] = 85.5m },
        LowStockProducts = { "Widget A" },
        Warnings = { "Stock is low, \"reorder\" soon" },
    };

    [Fact]
    public async Task Json_RoundTripsToEqualSummary()
    {
        var exporter = new JsonReportExporter(_options, _environment, NullLogger<JsonReportExporter>.Instance);

        await exporter.ExportAsync(CreateSummary());

        await using var stream = File.OpenRead(OutputPath("json"));
        var result = await JsonSerializer.DeserializeAsync<ReportSummary>(stream);
        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2025, 1, 1), result.From);
        Assert.Equal(new DateOnly(2025, 1, 5), result.To);
        Assert.Equal(805.5m, result.TotalRevenue);
        Assert.Equal(5, result.TotalOrders);
        Assert.Equal(4, result.UniqueCustomers);
        Assert.Equal(460m, result.RevenueByRegion["North"]);
        Assert.Equal(["Widget A"], result.LowStockProducts);
    }

    // de-DE uses ',' as decimal separator and dd.MM.yyyy dates; the file must not change.
    [Fact]
    public async Task Csv_UsesInvariantFormattingAndEscapesFields()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var exporter = new CsvReportExporter(_options, _environment, NullLogger<CsvReportExporter>.Instance);

            await exporter.ExportAsync(CreateSummary());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }

        var lines = await File.ReadAllLinesAsync(OutputPath("csv"));
        Assert.Equal("Section,Name,Value", lines[0]);
        Assert.Contains("Period,From,2025-01-01", lines);
        Assert.Contains("Totals,TotalRevenue,805.50", lines);
        Assert.Contains("Region,North,460.00", lines);
        Assert.Contains("Region,South,85.50", lines);
        Assert.Contains("LowStock,,Widget A", lines);
        Assert.Contains("Warning,,\"Stock is low, \"\"reorder\"\" soon\"", lines);
    }

    [Fact]
    public async Task FileExporter_RelativeOutputDirectory_IsUnderContentRoot()
    {
        var exporter = new CsvReportExporter(_options, _environment, NullLogger<CsvReportExporter>.Instance);

        await exporter.ExportAsync(CreateSummary());

        Assert.True(File.Exists(OutputPath("csv")));
    }

    // Regression: the file used to be created before rendering, so a failed export left an
    // empty file next to the good ones.
    [Fact]
    public async Task FileExporter_RenderFails_LeavesNoFile()
    {
        var exporter = new FailingFileExporter(_options, _environment);

        await Assert.ThrowsAsync<IOException>(() => exporter.ExportAsync(CreateSummary()));

        Assert.Empty(Directory.GetFiles(Path.Combine(_contentRoot, "out")));
    }

    [Fact]
    public async Task Pdf_LogsNotImplementedAndWritesNothing()
    {
        var logger = new FakeLogger<PdfReportExporter>();
        var exporter = new PdfReportExporter(logger);

        await exporter.ExportAsync(CreateSummary());

        Assert.False(Directory.Exists(_contentRoot));
        var record = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Contains("not implemented", record.Message);
    }

    // Writes part of the output, then fails, like an exporter that breaks halfway through.
    private sealed class FailingFileExporter(IOptions<ExportOptions> options, HostingEnvironment environment)
        : FileReportExporter(options, environment, NullLogger.Instance)
    {
        public override string Format => "failing";

        protected override string FileExtension => "txt";

        protected override async Task RenderAsync(ReportSummary summary, Stream output, CancellationToken cancellationToken)
        {
            await output.WriteAsync("partial"u8.ToArray(), cancellationToken);
            throw new IOException("simulated failure");
        }
    }
}
