namespace ReportService.Export;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    // Shared by Program.cs and the composition test so both use the same registrations.
    // To add a format: register its IReportExporter here and list it in Export:Formats.
    public static IServiceCollection AddReportExport(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<ExportOptions>()
            .Bind(configuration.GetSection(ExportOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.OutputDirectory), "Export:OutputDirectory must not be empty.")
            .Validate(o => o.Formats.Length > 0, "Export:Formats must list at least one format.")
            .Validate(o => o.Formats.All(f => !string.IsNullOrWhiteSpace(f)), "Export:Formats must not contain empty entries.")
            .ValidateOnStart();

        services.AddSingleton<IReportExporter, JsonReportExporter>();
        services.AddSingleton<IReportExporter, CsvReportExporter>();
        services.AddSingleton<IReportExporter, PdfReportExporter>();
        services.AddSingleton<ReportExportPipeline>();

        return services;
    }
}
