using MarketData.Configuration;
using MarketData.Services;
using MarketData.Telemetry;

namespace MarketData.Extensions;

/// <summary>
/// Extension methods for configuring application-specific services.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Adds custom telemetry services (metrics and activity sources).
    /// </summary>
    public static WebApplicationBuilder AddCustomTelemetry(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<MarketDataGeneratorServiceMetrics>();
        builder.Services.AddSingleton<MarketDataActivitySource>();

        return builder;
    }

    /// <summary>
    /// Adds market data generation services (simulators, managers, hosted service).
    /// </summary>
    public static WebApplicationBuilder AddMarketDataServices(this WebApplicationBuilder builder)
    {
        // Configure options
        builder.Services.Configure<MarketDataGeneratorOptions>(
            builder.Configuration.GetSection(MarketDataGeneratorOptions.SectionName));

        // Register price simulator and model management services
        builder.Services.AddSingleton<IPriceSimulatorFactory, PriceSimulatorFactory>();
        builder.Services.AddSingleton<IInstrumentModelManager, InstrumentModelManager>();
        builder.Services.AddSingleton<IDefaultModelConfigFactory, DefaultModelConfigFactory>();

        // Register background service
        builder.Services.AddHostedService<MarketDataGeneratorService>();

        return builder;
    }

    /// <summary>
    /// Adds web API services (controllers, OpenAPI, gRPC).
    /// </summary>
    public static WebApplicationBuilder AddWebApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddGrpc();

        return builder;
    }
}
