using System.Diagnostics;

namespace MarketData.Wpf.Client.Telemetry;

/// <summary>
/// Provides custom activity sources for distributed tracing in MarketData WPF client
/// </summary>
public class MarketDataClientActivitySource
{
    private readonly ActivitySource _source;

    public MarketDataClientActivitySource(string serviceName, string serviceVersion)
    {
        _source = new ActivitySource(serviceName, serviceVersion);
    }

    public ActivitySource Source => _source;

    /// <summary>
    /// Creates an activity for processing a received price update
    /// </summary>
    public Activity? StartPriceReceivedActivity(string instrument)
    {
        var activity = _source.StartActivity("PriceReceived", ActivityKind.Consumer);
        activity?.SetTag("instrument", instrument);
        activity?.SetTag("operation", "process_price_update");
        return activity;
    }

    /// <summary>
    /// Creates an activity for subscription initialization
    /// </summary>
    public Activity? StartSubscriptionActivity(string instrument)
    {
        var activity = _source.StartActivity("SubscribeToInstrument", ActivityKind.Client);
        activity?.SetTag("operation", "subscribe");
        activity?.SetTag("instrument", instrument);
        return activity;
    }

    /// <summary>
    /// Creates an activity for chart update operations
    /// </summary>
    public Activity? StartChartUpdateActivity(string instrument)
    {
        var activity = _source.StartActivity("UpdateChart", ActivityKind.Internal);
        activity?.SetTag("instrument", instrument);
        activity?.SetTag("operation", "update_candle_chart");
        return activity;
    }
}
