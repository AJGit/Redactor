using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace RedactorApi.Tests.Integration;

public class WebApiApplication(ITestOutputHelper testOutputHelper) : WebApplicationFactory<Program>
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.json");
            var inMemorySettings = new Dictionary<string, string>
            {
                // Assumes you have presidio running on localhost:7002
                { "Analyzer:BaseUrl", "http://localhost:7002" }
            };
            config.AddInMemoryCollection(inMemorySettings!);
        });

        builder.UseEnvironment("Testing");

        builder.ConfigureLogging(loggingBuilder =>
        {
            loggingBuilder.Services.AddSingleton<ILoggerProvider>(serviceProvider => new XUnitLoggerProvider(_testOutputHelper));
        });

        builder.ConfigureServices(services =>
        {
            // Create a new service provider.
            var serviceProvider = services.BuildServiceProvider();
        });
        return base.CreateHost(builder);
    }
}