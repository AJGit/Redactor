using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using RedactorApi.Analyzer;
using RedactorApi.FileScanners.Analyzers;
using RedactorApi.FileScanners;
using RedactorApi.Analyzer.Replacer;
using RedactorApi.Client;
using RedactorApi.Tasks;

namespace RedactorApi;

public static class Registry
{
    public static void MapIocServices(this IServiceCollection services)
    {
        services.AddTransient<IAnalyzer, Analyzer.Analyzer>();
        services.AddTransient<IFileScanner, PdfAnalyzer>();
        services.AddTransient<IFileScanner, ExcelAnalyzer>();
        services.AddTransient<IFileScanner, WordAnalyzer>();
        services.AddTransient<IFileScanner, PowerPointAnalyzer>();
        services.AddTransient<IFileScanner, TextAnalyzer>();
        services.AddTransient<IFileScanner, CsvAnalyzer>();
        services.AddTransient<IFileScanner, MarkDownAnalyzer>();
        services.AddTransient<IFileScanner, UnknownAnalyzer>();
        services.AddTransient<FileAnalyzerFactory>();
        // services.AddTransient<ILogUtils, LogUtils>();

        services.AddSingleton<IReplacer, HighestPriorityReplacer>();

        services.AddHostedService<FreeMemoryBackgroundService>();

        services.AddHttpClient<IPresidioClient, PresidioClient>(static (services, client) =>
        {
            var uri = services.GetRequiredService<IConfiguration>().GetValue<string>("Analyzer:BaseUrl");
            client.BaseAddress = new Uri(uri!);
        });
        // .AddStandardResilienceHandler();
    }

    public static void SetupKestrel(this IServiceCollection services)
    {
        const long maxRequestBodySize = 100 * 1024 * 1024; // 100MB
        // Configure Kestrel for larger file uploads
        services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = maxRequestBodySize;
        });
        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = maxRequestBodySize;
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartHeadersLengthLimit = int.MaxValue;
        });
    }
}
