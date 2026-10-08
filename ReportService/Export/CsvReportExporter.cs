namespace ReportService.Export;

using System.Globalization;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportService.Models;

// Writes the summary as one flat Section,Name,Value table so the nested parts
// (regions, low-stock products, warnings) fit in a single CSV file.
public class CsvReportExporter : FileReportExporter
{
    public CsvReportExporter(IOptions<ExportOptions> options, IHostEnvironment environment, ILogger<CsvReportExporter> logger)
        : base(options, environment, logger)
    {
    }

    public override string Format => ExportFormats.Csv;

    protected override string FileExtension => "csv";

    protected override async Task RenderAsync(ReportSummary summary, Stream output, CancellationToken cancellationToken)
    {
        // UTF-8 without BOM; leaveOpen because the base class owns the stream.
        await using var writer = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);

        async Task Row(string section, string name, string value) =>
            await writer.WriteLineAsync($"{Escape(section)},{Escape(name)},{Escape(value)}".AsMemory(), cancellationToken);

        await Row("Section", "Name", "Value");
        await Row("Period", "From", FormatDate(summary.From));
        await Row("Period", "To", FormatDate(summary.To));
        await Row("Totals", "TotalOrders", summary.TotalOrders.ToString(CultureInfo.InvariantCulture));
        await Row("Totals", "TotalRevenue", FormatMoney(summary.TotalRevenue));
        await Row("Totals", "UniqueCustomers", summary.UniqueCustomers.ToString(CultureInfo.InvariantCulture));

        foreach (var kv in summary.RevenueByRegion.OrderBy(k => k.Key, StringComparer.Ordinal))
            await Row("Region", kv.Key, FormatMoney(kv.Value));

        foreach (var product in summary.LowStockProducts)
            await Row("LowStock", "", product);

        foreach (var warning in summary.Warnings)
            await Row("Warning", "", warning);

        await writer.FlushAsync(cancellationToken);
    }

    // Invariant culture with fixed patterns: the file must read the same on every machine,
    // and Program.cs sets en-US only for console output. "0.00" does not depend on the
    // decimal's scale (60m and 60.000m both become "60.00").
    private static string FormatMoney(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string FormatDate(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // RFC 4180: quote fields containing a separator, quote or line break; double inner quotes.
    private static string Escape(string field) =>
        field.AsSpan().IndexOfAny(",\"\r\n") >= 0
            ? $"\"{field.Replace("\"", "\"\"")}\""
            : field;
}
