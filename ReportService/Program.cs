using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ReportService;
using ReportService.Export;
using ReportService.Services;

CultureInfo.CurrentCulture = new CultureInfo("en-US");

// FIX (config depends on working directory): was 'Host.CreateApplicationBuilder(args)'.
// Bug: appsettings.json was only found when the app was started from ReportService/.
// Cause: a console host's content root defaults to the current working directory, and
//        appsettings.json is read from the content root. From any other directory the file
//        was missing, so every setting silently fell back to its default (or failed validation).
// Fix: use the build output folder, where appsettings.json is copied, as the content root.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Services
    .AddOptions<ReportOptions>()
    .Bind(builder.Configuration.GetSection(ReportOptions.SectionName))
    .Validate(o => o.BatchSize > 0, "Report:BatchSize must be greater than 0.")
    .Validate(o => o.LowStockThreshold >= 0, "Report:LowStockThreshold must not be negative.");

// Singleton so its HttpClient is reused across reports.
builder.Services.AddSingleton<SalesDataClient>();
builder.Services.AddSingleton<MetricsCalculator>();
builder.Services.AddSingleton<ReportAggregator>();
builder.Services.AddReportExport(builder.Configuration);

using var host = builder.Build();
var aggregator = host.Services.GetRequiredService<ReportAggregator>();
var exportPipeline = host.Services.GetRequiredService<ReportExportPipeline>();
// Read now so invalid export settings stop the app before any report is generated.
var exportOptions = host.Services.GetRequiredService<IOptions<ExportOptions>>().Value;

// First report: first half of the sample dataset
var from = new DateOnly(2025, 1, 1);
var to   = new DateOnly(2025, 1, 5);

Console.WriteLine($"Generating report: {from:yyyy-MM-dd} to {to:yyyy-MM-dd}");
Console.WriteLine(new string('-', 50));

var report = await aggregator.GenerateReportAsync(from, to);

Console.WriteLine($"Total orders    : {report.TotalOrders}");
Console.WriteLine($"Total revenue   : {report.TotalRevenue:C}");
Console.WriteLine($"Unique customers: {report.UniqueCustomers}");
Console.WriteLine();

Console.WriteLine("Revenue by region:");
foreach (var kv in report.RevenueByRegion.OrderBy(k => k.Key))
    Console.WriteLine($"  {kv.Key,-10} {kv.Value:C}");

Console.WriteLine();
Console.WriteLine("Low-stock products:");
if (report.LowStockProducts.Count == 0)
    Console.WriteLine("  (none)");
else
    foreach (var p in report.LowStockProducts)
        Console.WriteLine($"  - {p}");

if (report.Warnings.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Warnings:");
    foreach (var w in report.Warnings)
        Console.WriteLine($"  ! {w}");
}

if (exportOptions.Enabled)
    await exportPipeline.ExportAsync(report, exportOptions.Formats);

// Second report: second half
Console.WriteLine();
var from2 = new DateOnly(2025, 1, 6);
var to2   = new DateOnly(2025, 1, 10);

Console.WriteLine($"Generating report: {from2:yyyy-MM-dd} to {to2:yyyy-MM-dd}");
Console.WriteLine(new string('-', 50));

var report2 = await aggregator.GenerateReportAsync(from2, to2);

Console.WriteLine($"Total orders    : {report2.TotalOrders}");
Console.WriteLine($"Total revenue   : {report2.TotalRevenue:C}");
Console.WriteLine($"Unique customers: {report2.UniqueCustomers}");

if (report2.Warnings.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Warnings:");
    foreach (var w in report2.Warnings)
        Console.WriteLine($"  ! {w}");
}

if (exportOptions.Enabled)
    await exportPipeline.ExportAsync(report2, exportOptions.Formats);
