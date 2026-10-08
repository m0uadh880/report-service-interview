namespace ReportService.Services;

using ReportService.Models;

public class MetricsCalculator
{
    public List<SalesRecord> FilterByDateRange(
        IEnumerable<SalesRecord> records,
        DateOnly from,
        DateOnly to)
    {
        // FIX (missing first day): the lower bound used '>' instead of '>='.
        // Bug: orders on the 'from' date were excluded from the report.
        // Cause: from.ToDateTime(TimeOnly.MinValue) is midnight, and an order dated exactly
        //        midnight on that day is not '>' it, so the first day's orders were dropped.
        // Fix: '>=' makes the range inclusive on both ends, matching the upper bound
        //      (TimeOnly.MaxValue with '<=').
        return records
            .Where(r => r.OrderDate >= from.ToDateTime(TimeOnly.MinValue)
                     && r.OrderDate <= to.ToDateTime(TimeOnly.MaxValue))
            .ToList();
    }

    public decimal CalculateTotalRevenue(IEnumerable<SalesRecord> records)
    {
        return records.Sum(r => r.Amount);
    }

    public Dictionary<string, decimal> GroupByRegion(IEnumerable<SalesRecord> records)
    {
        return records
            .GroupBy(r => r.Region)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));
    }

    public int CountUniqueCustomers(IEnumerable<SalesRecord> records)
    {
        return records.Select(r => r.CustomerId).Distinct().Count();
    }
}
