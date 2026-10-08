namespace ReportService.Tests;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;
using ReportService.Export;

// Uses the same AddReportExport call as Program.cs, so a missing or broken registration fails here.
public class ExportCompositionTests
{
    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment { ContentRootPath = Path.GetTempPath() });
        services.AddReportExport(configuration);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public void AddReportExport_RegistersJsonCsvAndPdf()
    {
        using var provider = BuildProvider(new() { ["Export:Formats:0"] = "json" });

        var pipeline = provider.GetRequiredService<ReportExportPipeline>();

        Assert.Equal(
            [ExportFormats.Csv, ExportFormats.Json, ExportFormats.Pdf],
            pipeline.AvailableFormats.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void AddReportExport_NoFormatsConfigured_FailsStartupValidation()
    {
        using var provider = BuildProvider(new());

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IStartupValidator>().Validate());
        Assert.Contains("Export:Formats", ex.Message);
    }
}
