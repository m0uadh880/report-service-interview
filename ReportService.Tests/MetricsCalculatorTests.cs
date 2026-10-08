namespace ReportService.Tests;

using ReportService.Models;
using ReportService.Services;

public class MetricsCalculatorTests
{
    private readonly MetricsCalculator _calculator = new();

    private static SalesRecord Order(int id, DateTime date, decimal amount = 10m, int customerId = 1, string region = "North") =>
        new() { OrderId = id, CustomerId = customerId, Amount = amount, OrderDate = date, Region = region };

    // Regression: the lower bound used '>' so orders at midnight on the 'from' day were dropped.
    [Fact]
    public void FilterByDateRange_IncludesOrderAtMidnightOnFirstDay()
    {
        var records = new[] { Order(1, new DateTime(2025, 1, 1)) };

        var result = _calculator.FilterByDateRange(records, new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 5));

        Assert.Single(result);
    }

    [Fact]
    public void FilterByDateRange_IncludesOrderLateOnLastDay()
    {
        var records = new[] { Order(1, new DateTime(2025, 1, 5, 23, 59, 59)) };

        var result = _calculator.FilterByDateRange(records, new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 5));

        Assert.Single(result);
    }

    [Fact]
    public void FilterByDateRange_ExcludesOrdersOutsideRange()
    {
        var records = new[]
        {
            Order(1, new DateTime(2024, 12, 31, 23, 59, 59)),
            Order(2, new DateTime(2025, 1, 3)),
            Order(3, new DateTime(2025, 1, 6)),
        };

        var result = _calculator.FilterByDateRange(records, new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 5));

        Assert.Equal(new[] { 2 }, result.Select(r => r.OrderId));
    }

    [Fact]
    public void CalculateTotalRevenue_SumsAmounts()
    {
        var records = new[]
        {
            Order(1, new DateTime(2025, 1, 1), amount: 120.00m),
            Order(2, new DateTime(2025, 1, 2), amount: 85.50m),
        };

        Assert.Equal(205.50m, _calculator.CalculateTotalRevenue(records));
    }

    [Fact]
    public void CalculateTotalRevenue_EmptyInput_ReturnsZero()
    {
        Assert.Equal(0m, _calculator.CalculateTotalRevenue([]));
    }

    [Fact]
    public void GroupByRegion_SumsAmountPerRegion()
    {
        var records = new[]
        {
            Order(1, new DateTime(2025, 1, 1), amount: 100m, region: "North"),
            Order(2, new DateTime(2025, 1, 2), amount: 50m,  region: "South"),
            Order(3, new DateTime(2025, 1, 3), amount: 25m,  region: "North"),
        };

        var result = _calculator.GroupByRegion(records);

        Assert.Equal(2, result.Count);
        Assert.Equal(125m, result["North"]);
        Assert.Equal(50m, result["South"]);
    }

    [Fact]
    public void CountUniqueCustomers_CountsEachCustomerOnce()
    {
        var records = new[]
        {
            Order(1, new DateTime(2025, 1, 1), customerId: 10),
            Order(2, new DateTime(2025, 1, 2), customerId: 11),
            Order(3, new DateTime(2025, 1, 3), customerId: 10),
        };

        Assert.Equal(2, _calculator.CountUniqueCustomers(records));
    }
}
