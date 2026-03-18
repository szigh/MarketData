using System.Diagnostics.Metrics;
using MarketData.Configuration;
using Microsoft.Extensions.Options;

namespace MarketData.Telemetry;

/// <summary>
/// Provides custom metrics for the MarketData application
/// </summary>
public class MarketDataMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _pricesGeneratedCounter;
    private readonly Counter<long> _pricesSavedCounter;
    private readonly Counter<long> _pricesPublishedCounter;
    private readonly Histogram<double> _priceGenerationLatency;
    private readonly Histogram<double> _databaseSaveLatency;
    private readonly Histogram<double> _grpcPublishLatency;
    private readonly Counter<long> _errorCounter;

    private int _activeInstruments;

    public MarketDataMetrics(IMeterFactory meterFactory, IOptions<OpenTelemetryOptions> options)
    {
        var serviceName = options.Value.ServiceName;
        _meter = meterFactory.Create(serviceName);

        _pricesGeneratedCounter = _meter.CreateCounter<long>(
            "marketdata.prices.generated",
            unit: "prices",
            description: "Total number of prices generated");

        _pricesSavedCounter = _meter.CreateCounter<long>(
            "marketdata.prices.saved",
            unit: "prices",
            description: "Total number of prices saved to database");

        _pricesPublishedCounter = _meter.CreateCounter<long>(
            "marketdata.prices.published",
            unit: "prices",
            description: "Total number of prices published via gRPC");

        _priceGenerationLatency = _meter.CreateHistogram<double>(
            "marketdata.price_generation.duration",
            unit: "ms",
            description: "Time taken to generate a price");

        _databaseSaveLatency = _meter.CreateHistogram<double>(
            "marketdata.database.save.duration",
            unit: "ms",
            description: "Time taken to save prices to database");

        _grpcPublishLatency = _meter.CreateHistogram<double>(
            "marketdata.grpc.publish.duration",
            unit: "ms",
            description: "Time taken to publish prices via gRPC");

        // ObservableGauge uses callback - no need to store the gauge itself
        _meter.CreateObservableGauge<int>(
            "marketdata.instruments.active",
            () => _activeInstruments,
            unit: "instruments",
            description: "Number of active instruments");

        _errorCounter = _meter.CreateCounter<long>(
            "marketdata.errors",
            unit: "errors",
            description: "Total number of errors");
    }

    public void RecordPriceGenerated(string instrument)
    {
        _pricesGeneratedCounter.Add(1, new KeyValuePair<string, object?>("instrument", instrument));
    }

    public void RecordPricesSaved(int count, string instrument)
    {
        _pricesSavedCounter.Add(count, new KeyValuePair<string, object?>("instrument", instrument));
    }

    public void RecordPricesPublished(int count, string instrument)
    {
        _pricesPublishedCounter.Add(count, new KeyValuePair<string, object?>("instrument", instrument));
    }

    public void RecordPriceGenerationLatency(double milliseconds, string instrument)
    {
        _priceGenerationLatency.Record(milliseconds, new KeyValuePair<string, object?>("instrument", instrument));
    }

    public void RecordDatabaseSaveLatency(double milliseconds)
    {
        _databaseSaveLatency.Record(milliseconds);
    }

    public void RecordGrpcPublishLatency(double milliseconds, string instrument)
    {
        _grpcPublishLatency.Record(milliseconds, new KeyValuePair<string, object?>("instrument", instrument));
    }

    public void SetActiveInstruments(int count)
    {
        _activeInstruments = count;
    }

    public void RecordError(string errorType, string? operation = null)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("error.type", errorType),
            new KeyValuePair<string, object?>("operation", operation)
        };
        _errorCounter.Add(1, tags);
    }
}
