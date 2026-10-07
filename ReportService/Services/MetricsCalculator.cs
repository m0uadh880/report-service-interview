namespace ReportService.Services;

using ReportService.Models;

public class MetricsCalculator
{
    public List<SalesRecord> FilterByDateRange(
        IEnumerable<SalesRecord> records,
        DateOnly from,
        DateOnly to)
    {
        return records
            .Where(r => r.OrderDate > from.ToDateTime(TimeOnly.MinValue)
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
