namespace ReportService.Export;

using Microsoft.Extensions.Logging;
using ReportService.Models;

// Entry point for exporting a summary to several formats in one call.
// It only knows IReportExporter, so new formats need no change here.
public class ReportExportPipeline
{
    private readonly Dictionary<string, IReportExporter> _exporters = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ReportExportPipeline> _logger;

    public ReportExportPipeline(IEnumerable<IReportExporter> exporters, ILogger<ReportExportPipeline> logger)
    {
        _logger = logger;

        foreach (var exporter in exporters)
        {
            // Two exporters for one format is a registration mistake; fail at startup
            // rather than silently picking one.
            if (!_exporters.TryAdd(exporter.Format, exporter))
                throw new InvalidOperationException(
                    $"More than one exporter is registered for format '{exporter.Format}': " +
                    $"{_exporters[exporter.Format].GetType().Name} and {exporter.GetType().Name}.");
        }
    }

    public IReadOnlyCollection<string> AvailableFormats => _exporters.Keys;

    // Exports to each requested format in turn. A failing or unknown format is logged and
    // skipped; the remaining formats still run. Only cancellation by the caller is rethrown.
    public async Task ExportAsync(ReportSummary summary, IEnumerable<string> formats, CancellationToken cancellationToken = default)
    {
        foreach (var format in formats.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!_exporters.TryGetValue(format, out var exporter))
            {
                _logger.LogWarning("No exporter for format {Format}; available formats: {Available}",
                    format, string.Join(", ", _exporters.Keys));
                continue;
            }

            try
            {
                await exporter.ExportAsync(summary, cancellationToken);
            }
            // An OperationCanceledException from the exporter's own timeout is a failure of
            // that format only. It propagates only when the caller's token was cancelled.
            catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                _logger.LogError(ex, "Export to {Format} failed for report {From}..{To}", format, summary.From, summary.To);
            }
        }
    }
}
