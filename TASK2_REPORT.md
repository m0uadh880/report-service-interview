# Task 2: Export pipeline

## Design

Each format is a strategy behind one interface. DI supplies all of them to a pipeline that finds them by name.

| Component                        | Role                                                                                                                   |
| -------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| `IReportExporter`                | One format. Exposes `Format` (e.g. `"json"`) and `ExportAsync(summary, ct)`                                            |
| `FileReportExporter`             | Abstract base for formats that write one file per report. Subclasses only implement `RenderAsync(summary, Stream, ct)` |
| `JsonReportExporter`             | Indented JSON via `System.Text.Json`                                                                                   |
| `CsvReportExporter`              | Flat `Section,Name,Value` table, invariant formatting, RFC 4180 escaping                                               |
| `PdfReportExporter`              | Stub: logs a "not implemented" warning and returns                                                                     |
| `ReportExportPipeline`           | Entry point: `ExportAsync(summary, formats, ct)` runs each requested format and isolates failures                      |
| `ExportFormats`                  | Format name constants used by the exporters                                                                            |
| `ExportOptions`                  | `Export:OutputDirectory` and `Export:Formats` from `appsettings.json`, validated                                       |
| `AddReportExport(configuration)` | DI registrations, shared by `Program.cs` and the composition test                                                      |

`Program.cs` exports each report to the formats in `Export:Formats` (`json`, `csv`, `pdf` by default). Files go to `ReportService/exports/report_{from}_{to}.{ext}`.

## Adding a format

1. Write a class implementing `IReportExporter`, or deriving from `FileReportExporter` if it writes a file.
2. Register it in `AddReportExport`: `services.AddSingleton<IReportExporter, XmlReportExporter>();`
3. Add its name to `Export:Formats` in `appsettings.json`.

The pipeline, the other exporters and `Program.cs` do not change.

## Error handling

- **One format fails:** the exception is logged at Error with the format and report range, and the next format still runs. Nothing is thrown to the caller.
- **Unknown format requested:** logged at Warning with the list of available formats, then skipped.
- **Same format requested twice** (`json`, `JSON`): exported once.
- **Two exporters registered for one format:** the pipeline constructor throws at startup.
- **Failed file export:** the file is rendered to a `.tmp` file and moved into place only on success, so a failure never leaves an empty or truncated file. I found this during testing: the first version created the file before rendering, and a simulated CSV failure left 0-byte `.csv` files.
- **Invalid settings** (blank output directory, no formats, blank format entry): the app stops at startup with a clear message.

## Unit tests

13 new tests, **35 in total, all passing**. `FakeLogger` (from `Microsoft.Extensions.Diagnostics.Testing`) is used where logs are asserted.

| Test class                  | Tests | What it covers                                                                                                                                                                                                                                           |
| --------------------------- | ----- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ReportExportPipelineTests` | 6     | Failing exporter does not stop others and is logged at Error; exporter's own `OperationCanceledException` does not stop others; caller cancellation propagates; unknown format logged; case-insensitive dedup; duplicate registration throws             |
| `ExporterTests`             | 5     | JSON round-trips to an equal `ReportSummary`; CSV uses invariant formatting under `de-DE` and escapes commas/quotes; relative output directory resolves under the content root; failed render leaves no file; PDF stub logs a warning and writes nothing |
| `ExportCompositionTests`    | 2     | `AddReportExport` resolves the pipeline with json/csv/pdf; empty `Export:Formats` fails startup validation                                                                                                                                               |

## Verification

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: 35/35 pass.
- `dotnet run`: both reports print, JSON and CSV files are written for each (4 files), and the PDF stub logs its warning.
- CSV exporter temporarily made to throw: error logged for each report, JSON still written, no CSV file left behind, app exits 0. Change reverted.
- `dotnet run -- --Export:Formats:0=`: app stops with `OptionsValidationException: Export:Formats must not contain empty entries.`
