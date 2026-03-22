using System.Diagnostics;
using MarketData.Client.Shared.Configuration;
using Microsoft.Extensions.Options;

namespace MarketData.Client.Telemetry;

/// <summary>
/// Provides custom activity sources for distributed tracing in MarketData client
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
    public Activity? StartSubscriptionActivity(string[] instruments)
    {
        var activity = _source.StartActivity("SubscribeToInstruments", ActivityKind.Client);
        activity?.SetTag("operation", "subscribe");
        activity?.SetTag("instrument.count", instruments.Length);
        activity?.SetTag("instruments", string.Join(",", instruments));
        return activity;
    }
}
