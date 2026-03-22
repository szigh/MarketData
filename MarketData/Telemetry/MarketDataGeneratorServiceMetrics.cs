using System.Diagnostics;
using System.Diagnostics.Metrics;
using MarketData.Configuration;
using Microsoft.Extensions.Options;

namespace MarketData.Telemetry;

/// <summary>
/// Provides custom metrics for the MarketData application
/// </summary>
public class MarketDataGeneratorServiceMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _pricesGeneratedCounter;
    private readonly Counter<long> _pricesSavedCounter;
    private readonly Counter<long> _pricesPublishedCounter;
    private readonly Histogram<double> _priceGenerationLatency;
    private readonly Histogram<double> _databaseSaveLatency;
    private readonly Histogram<double> _grpcPublishLatency;
    private readonly Counter<long> _errorCounter;

    public MarketDataGeneratorServiceMetrics(IMeterFactory meterFactory, IOptions<OpenTelemetryOptions> options)
    {
        var serviceName = options.Value.ServiceName;
        _meter = meterFactory.Create(serviceName);

        _pricesGeneratedCounter = _meter.CreateCounter<long>(
            "marketdata.generator.prices.generated",
            unit: "prices",
            description: "Total number of prices generated");

        _pricesSavedCounter = _meter.CreateCounter<long>(
            "marketdata.generator.prices.saved",
            unit: "prices",
            description: "Total number of prices saved to database");

        _pricesPublishedCounter = _meter.CreateCounter<long>(
            "marketdata.generator.prices.published",
            unit: "prices",
            description: "Total number of prices published via gRPC");

        _priceGenerationLatency = _meter.CreateHistogram<double>(
            "marketdata.generator.price_generation.duration",
            unit: "ms",
            description: "Time taken to generate a price");

        _databaseSaveLatency = _meter.CreateHistogram<double>(
            "marketdata.generator.database.save.duration",
            unit: "ms",
            description: "Time taken to save prices to database");

        _grpcPublishLatency = _meter.CreateHistogram<double>(
            "marketdata.generator.grpc.publish.duration",
            unit: "ms",
            description: "Time taken to publish prices via gRPC");

        _errorCounter = _meter.CreateCounter<long>(
            "marketdata.generator.errors",
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

    public TrackedRequestDuration RecordPriceGenerationLatency(string instrument)
    {
        return new TrackedRequestDuration(_priceGenerationLatency, 
            [new KeyValuePair<string, object?>("instrument", instrument)]);
    }

    public TrackedRequestDuration RecordDatabaseSaveLatency(string instrument)
    {
        return new TrackedRequestDuration(_databaseSaveLatency, 
            [new KeyValuePair<string, object?>("instrument", instrument)]);
    }

    public TrackedRequestDuration RecordGrpcPublishLatency(string instrument)
    {
        return new TrackedRequestDuration(_grpcPublishLatency, 
            [new KeyValuePair<string, object?>("instrument", instrument)]);
    }

    public void RecordError(string errorType, Exception? exception = null, string? operation = null)
    {
        var tags = new List<KeyValuePair<string, object?>>(6)
        {
            new("error.type", errorType)
        };
        if(operation != null)
        {
            tags.Add(new KeyValuePair<string, object?>("operation", operation));
        }
        if(exception != null)
        {
            tags.Add(new KeyValuePair<string, object?>("exception.type", exception.GetType().FullName));
            tags.Add(new KeyValuePair<string, object?>("exception.message", exception.Message));
            tags.Add(new KeyValuePair<string, object?>("exception.stacktrace", exception.StackTrace));
        }
        _errorCounter.Add(1, tags.ToArray());
    }
}

public class TrackedRequestDuration : IDisposable
{
    private readonly long _requestStartTime = Stopwatch.GetTimestamp();
    private readonly Histogram<double> _histogram;
    private readonly KeyValuePair<string, object?>[] _tags;

    public TrackedRequestDuration(Histogram<double> histogram, KeyValuePair<string, object?>[] tags)
    {
        _histogram = histogram;
        _tags = tags;
    }

    public void Dispose()
    {
        var elapsedMs = (Stopwatch.GetTimestamp() - _requestStartTime) * 1000.0 / Stopwatch.Frequency;
        _histogram.Record(elapsedMs, _tags);
    }
}