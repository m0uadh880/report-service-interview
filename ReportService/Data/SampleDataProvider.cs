namespace ReportService.Data;

using ReportService.Models;

public static class SampleDataProvider
{
    public static List<SalesRecord> GetSalesRecords()
    {
        return new List<SalesRecord>
        {
            new() { OrderId = 1,  CustomerId = 10, Amount = 120.00m, OrderDate = new DateTime(2025, 1, 1),  Region = "North" },
            new() { OrderId = 2,  CustomerId = 11, Amount = 85.50m,  OrderDate = new DateTime(2025, 1, 2),  Region = "South" },
            new() { OrderId = 3,  CustomerId = 12, Amount = 340.00m, OrderDate = new DateTime(2025, 1, 3),  Region = "North" },
            new() { OrderId = 4,  CustomerId = 10, Amount = 60.00m,  OrderDate = new DateTime(2025, 1, 4),  Region = "East"  },
            new() { OrderId = 5,  CustomerId = 13, Amount = 200.00m, OrderDate = new DateTime(2025, 1, 5),  Region = "West"  },
            new() { OrderId = 6,  CustomerId = 14, Amount = 175.00m, OrderDate = new DateTime(2025, 1, 6),  Region = "South" },
            new() { OrderId = 7,  CustomerId = 15, Amount = 90.00m,  OrderDate = new DateTime(2025, 1, 7),  Region = "North" },
            new() { OrderId = 8,  CustomerId = 11, Amount = 450.00m, OrderDate = new DateTime(2025, 1, 8),  Region = "East"  },
            new() { OrderId = 9,  CustomerId = 16, Amount = 310.00m, OrderDate = new DateTime(2025, 1, 9),  Region = "West"  },
            new() { OrderId = 10, CustomerId = 10, Amount = 55.00m,  OrderDate = new DateTime(2025, 1, 10), Region = "South" },
        };
    }

    public static List<InventorySnapshot> GetInventorySnapshots()
    {
        return new List<InventorySnapshot>
        {
            new() { ProductId = 1, ProductName = "Widget A",   StockLevel = 3,   SnapshotDate = new DateTime(2025, 1, 10) },
            new() { ProductId = 2, ProductName = "Widget B",   StockLevel = 150, SnapshotDate = new DateTime(2025, 1, 10) },
            new() { ProductId = 3, ProductName = "Gadget Pro", StockLevel = 0,   SnapshotDate = new DateTime(2025, 1, 10) },
            new() { ProductId = 4, ProductName = "Gadget Lite", StockLevel = 8,  SnapshotDate = new DateTime(2025, 1, 10) },
            new() { ProductId = 5, ProductName = "Component X", StockLevel = 42, SnapshotDate = new DateTime(2025, 1, 10) },
        };
    }
}
