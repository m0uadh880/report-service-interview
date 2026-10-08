namespace ReportService;

public class ExportOptions
{
    public const string SectionName = "Export";

    // Set to false to skip exporting (e.g. `dotnet run -- --Export:Enabled=false`).
    public bool Enabled { get; set; } = true;

    // Relative paths are resolved against the host's content root, not the working directory.
    public string OutputDirectory { get; set; } = "exports";

    // Formats exported after each report. Names are matched case-insensitively.
    public string[] Formats { get; set; } = [];
}
