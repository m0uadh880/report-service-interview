using System.Globalization;
using ReportService.Services;

CultureInfo.CurrentCulture = new CultureInfo("en-US");

var aggregator = new ReportAggregator();

// First report: first half of the sample dataset
var from = new DateOnly(2025, 1, 1);
var to   = new DateOnly(2025, 1, 5);

Console.WriteLine($"Generating report: {from:yyyy-MM-dd} to {to:yyyy-MM-dd}");
Console.WriteLine(new string('-', 50));

var report = aggregator.GenerateReport(from, to);

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

// Second report: second half
Console.WriteLine();
var from2 = new DateOnly(2025, 1, 6);
var to2   = new DateOnly(2025, 1, 10);

Console.WriteLine($"Generating report: {from2:yyyy-MM-dd} to {to2:yyyy-MM-dd}");
Console.WriteLine(new string('-', 50));

var report2 = aggregator.GenerateReport(from2, to2);

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
