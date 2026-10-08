# Task 1: ReportService fixes

## Bugs fixed

| #   | Bug                                             | Effect                                                       | Fix                                                                             |
| --- | ----------------------------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------------------------- |
| 1   | Date filter used `>` instead of `>=`            | Orders on the first day of the range were left out           | `>=`, so the range now includes both ends                                       |
| 2   | A new `HttpClient` for every batch              | Socket exhaustion under load                                 | One reused `HttpClient` per `SalesDataClient`                                   |
| 3   | Warnings kept in a `static` list                | Each report repeated earlier reports' warnings               | Warnings are stored per report in `summary.Warnings`                            |
| 4   | `.Result` and `.Wait()` on async code           | Thread-pool starvation, and possible deadlocks               | `GenerateReportAsync` with `await`                                              |
| 5   | Enrichers ran in parallel on shared collections | Lost or corrupted data, and output that changed between runs | Enrichers run one after another                                                 |
| 6   | Empty `catch` in the revenue enricher           | A failure showed up as $0 in sales, with nothing logged      | A shared wrapper logs the error with `ILogger` and adds a warning to the report |

Each fix has a comment in the code explaining the bug, why it caused the problem, and how the fix solves it.

## Improvements

- **Dependency injection:** dependencies come in through the constructor instead of `new`, wired up with the generic host.
- **Configuration:** `BatchSize` and `LowStockThreshold` are in `appsettings.json` and checked for valid values.
