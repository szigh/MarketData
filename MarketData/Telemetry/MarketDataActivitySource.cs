using System.Diagnostics;
using MarketData.Configuration;
using Microsoft.Extensions.Options;

namespace MarketData.Telemetry;

/// <summary>
/// Provides custom activity sources for distributed tracing in MarketData
/// </summary>
public class MarketDataActivitySource
{
    private readonly ActivitySource _source;

    public MarketDataActivitySource(IOptions<OpenTelemetryOptions> options)
    {
        var serviceName = options.Value.ServiceName;
        var serviceVersion = options.Value.ServiceVersion;
        _source = new ActivitySource(serviceName, serviceVersion);
    }

    public ActivitySource Source => _source;

    /// <summary>
    /// Creates an activity for price generation operations
    /// </summary>
    public Activity? StartPriceGenerationActivity(string instrument)
    {
        var activity = _source.StartActivity("PriceGeneration", ActivityKind.Internal);
        activity?.SetTag("instrument", instrument);
        activity?.SetTag("operation", "generate_price");
        return activity;
    }

    /// <summary>
    /// Creates an activity for database save operations
    /// </summary>
    public Activity? StartDatabaseSaveActivity(string instrument, int priceCount)
    {
        var activity = _source.StartActivity("DatabaseSave", ActivityKind.Internal);
        activity?.SetTag("instrument", instrument);
        activity?.SetTag("operation", "save_prices");
        activity?.SetTag("price.count", priceCount);
        return activity;
    }

    /// <summary>
    /// Creates an activity for gRPC publish operations
    /// </summary>
    public Activity? StartGrpcPublishActivity(string instrument, int priceCount)
    {
        var activity = _source.StartActivity("GrpcPublish", ActivityKind.Internal);
        activity?.SetTag("instrument", instrument);
        activity?.SetTag("operation", "publish_prices");
        activity?.SetTag("price.count", priceCount);
        return activity;
    }

    /// <summary>
    /// Creates an activity for instrument initialization
    /// </summary>
    public Activity? StartInstrumentInitActivity(string instrument)
    {
        var activity = _source.StartActivity("InstrumentInitialization", ActivityKind.Internal);
        activity?.SetTag("instrument", instrument);
        activity?.SetTag("operation", "initialize_instrument");
        return activity;
    }

    /// <summary>
    /// Creates an activity for configuration changes
    /// </summary>
    public Activity? StartConfigurationChangeActivity(string instrument, string changeType)
    {
        var activity = _source.StartActivity("ConfigurationChange", ActivityKind.Internal);
        activity?.SetTag("instrument", instrument);
        activity?.SetTag("change.type", changeType);
        activity?.SetTag("operation", "config_change");
        return activity;
    }
}
