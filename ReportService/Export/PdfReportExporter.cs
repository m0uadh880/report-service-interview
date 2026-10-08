namespace ReportService.Export;

using Microsoft.Extensions.Logging;
using ReportService.Models;

// Stub: logs instead of throwing, so requesting "pdf" never breaks other exports.
// A real implementation would derive from FileReportExporter and render to the stream.
public class PdfReportExporter : IReportExporter
{
    private readonly ILogger<PdfReportExporter> _logger;

    public PdfReportExporter(ILogger<PdfReportExporter> logger)
    {
        _logger = logger;
    }

    public string Format => ExportFormats.Pdf;

    public Task ExportAsync(ReportSummary summary, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("PDF export is not implemented yet; skipping report {From}..{To}", summary.From, summary.To);
        return Task.CompletedTask;
    }
}
