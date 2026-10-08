namespace ReportService.Export;

using ReportService.Models;

// One implementation per output format. ReportExportPipeline finds exporters by Format,
// so a new format is a new class plus a DI registration; no existing class changes.
public interface IReportExporter
{
    // Format name used by callers, e.g. "json". Matched case-insensitively.
    string Format { get; }

    Task ExportAsync(ReportSummary summary, CancellationToken cancellationToken = default);
}
