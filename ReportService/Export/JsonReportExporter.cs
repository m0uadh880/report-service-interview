namespace ReportService.Export;

using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportService.Models;

public class JsonReportExporter : FileReportExporter
{
    // Cached: JsonSerializerOptions builds per-type metadata on first use.
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public JsonReportExporter(IOptions<ExportOptions> options, IHostEnvironment environment, ILogger<JsonReportExporter> logger)
        : base(options, environment, logger)
    {
    }

    public override string Format => ExportFormats.Json;

    protected override string FileExtension => "json";

    protected override Task RenderAsync(ReportSummary summary, Stream output, CancellationToken cancellationToken) =>
        JsonSerializer.SerializeAsync(output, summary, SerializerOptions, cancellationToken);
}
