namespace ReportService.Export;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportService.Models;

// Base class for formats that write one file per report. Subclasses only render to a
// stream, so binary formats (a real PDF, XLSX) can reuse it as well as text formats.
public abstract class FileReportExporter : IReportExporter
{
    private readonly string _outputDirectory;
    private readonly ILogger _logger;

    protected FileReportExporter(IOptions<ExportOptions> options, IHostEnvironment environment, ILogger logger)
    {
        // Path.Combine returns an absolute OutputDirectory unchanged; a relative one is
        // resolved against the content root so the result does not depend on the working directory.
        _outputDirectory = Path.Combine(environment.ContentRootPath, options.Value.OutputDirectory);
        _logger = logger;
    }

    public abstract string Format { get; }

    protected abstract string FileExtension { get; }

    protected abstract Task RenderAsync(ReportSummary summary, Stream output, CancellationToken cancellationToken);

    public async Task ExportAsync(ReportSummary summary, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_outputDirectory);

        // One file per date range. Exporting the same range again overwrites the earlier file.
        var path = Path.Combine(_outputDirectory, $"report_{summary.From:yyyyMMdd}_{summary.To:yyyyMMdd}.{FileExtension}");

        // Render to a temporary file and move it into place only on success, so a failed or
        // cancelled export never leaves an empty or truncated file that looks like a real one.
        var tempPath = path + ".tmp";
        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await RenderAsync(summary, stream, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            File.Delete(tempPath);
            throw;
        }

        _logger.LogInformation("Exported {Format} for report {From}..{To} to {Path}", Format, summary.From, summary.To, path);
    }
}
