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

## Unit tests

Added a new xUnit project, `ReportService.Tests`, with **22 tests, all passing**. Run them with `dotnet test`.

| Test class | Tests | What it covers |
|---|---|---|
| `MetricsCalculatorTests` | 7 | Date-range boundaries (regression for bug #1), revenue totals, grouping by region, unique customers |
| `ReportAggregatorTests` | 10 | Expected totals for both sample periods, totals unchanged across batch sizes, no warnings repeated across reports (regression for bug #3), empty date range, configured low-stock threshold |
| `SalesDataClientTests` | 5 | Every record returned exactly once for any batch size, including a partial last batch |

## Improvements

- **Dependency injection:** dependencies come in through the constructor instead of `new`, wired up with the generic host.
- **Configuration:** `BatchSize` and `LowStockThreshold` are in `appsettings.json` and checked for valid values.

## Verification

The build passes with 0 warnings and all 22 unit tests pass. Both sample reports now include day 1 and show only their own warnings. A config override takes effect, and an invalid `BatchSize` stops the app with a clear message.
