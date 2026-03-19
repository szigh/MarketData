using MarketData.Models;

namespace MarketData.Services;

/// <summary>
/// Demonstrates structured logging for market data events
/// </summary>
public class MarketDataLogger
{
    private readonly ILogger<MarketDataLogger> _logger;

    public MarketDataLogger(ILogger<MarketDataLogger> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs a price update with all relevant context
    /// </summary>
    public void LogPriceUpdate(Instrument instrument, double oldPrice, double newPrice)
    {
        var change = newPrice - oldPrice;
        var changePercent = oldPrice != 0 ? (change / oldPrice) : 0;

        _logger.LogInformation(
            "Price Update | {Symbol} | {OldPrice:F4} → {NewPrice:F4} | Δ {Change:F4} ({ChangePercent:P2}) | Model: {ModelType}",
            instrument.Name,
            oldPrice,
            newPrice,
            change,
            changePercent,
            instrument.ModelType);
    }

    /// <summary>
    /// Logs a batch of price updates as structured data
    /// </summary>
    public void LogPriceUpdateBatch(IEnumerable<(Instrument instrument, double oldPrice, double newPrice)> updates)
    {
        var updateData = updates.Select(u => new
        {
            Symbol = u.instrument.Name,
            OldPrice = u.oldPrice,
            NewPrice = u.newPrice,
            Change = u.newPrice - u.oldPrice,
            ChangePercent = u.oldPrice != 0 ? (u.newPrice - u.oldPrice) / u.oldPrice : 0,
            ModelType = u.instrument.ModelType,
            Timestamp = DateTime.UtcNow
        }).ToArray();

        _logger.LogInformation(
            "Batch price update: {Count} instruments updated. {@Updates}",
            updateData.Length,
            updateData);
    }

    /// <summary>
    /// Logs market statistics in a queryable format
    /// </summary>
    public void LogMarketStatistics(
        IEnumerable<Instrument> instruments,
        Dictionary<string, double> currentPrices)
    {
        var stats = new
        {
            TotalInstruments = instruments.Count(),
            ByModel = instruments
                .GroupBy(i => i.ModelType)
                .Select(g => new { ModelType = g.Key, Count = g.Count() })
                .ToArray(),
            PriceRange = new
            {
                Min = currentPrices.Values.Min(),
                Max = currentPrices.Values.Max(),
                Average = currentPrices.Values.Average()
            },
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Market statistics: {@Statistics}", stats);

        // In Seq, query examples:
        // - Statistics.TotalInstruments > 100
        // - Statistics.ByModel[?].ModelType = 'RandomMultiplicative'
        // - Statistics.PriceRange.Average > 100
    }

    /// <summary>
    /// Logs instrument configuration as a table
    /// </summary>
    public void LogInstrumentConfiguration(Instrument instrument)
    {
        object? config = instrument.ModelType switch
        {
            "RandomMultiplicative" => new
            {
                StandardDeviation = instrument.RandomMultiplicativeConfig?.StandardDeviation,
                Mean = instrument.RandomMultiplicativeConfig?.Mean
            },
            "MeanReverting" => new
            {
                Mean = instrument.MeanRevertingConfig?.Mean,
                Kappa = instrument.MeanRevertingConfig?.Kappa,
                Sigma = instrument.MeanRevertingConfig?.Sigma,
                Dt = instrument.MeanRevertingConfig?.Dt
            },
            _ => null
        };

        _logger.LogInformation(
            "Instrument configuration: {Name} ({ModelType}) {@Config}",
            instrument.Name,
            instrument.ModelType,
            config);
    }

    /// <summary>
    /// Logs performance metrics for a generation cycle
    /// </summary>
    public void LogGenerationCycleMetrics(int instrumentCount, TimeSpan duration, int dbWrites)
    {
        var metrics = new Dictionary<string, object>
        {
            ["InstrumentCount"] = instrumentCount,
            ["TotalDuration_ms"] = duration.TotalMilliseconds,
            ["AvgTimePerInstrument_ms"] = duration.TotalMilliseconds / instrumentCount,
            ["DatabaseWrites"] = dbWrites,
            ["Throughput_PerSecond"] = instrumentCount / duration.TotalSeconds,
            ["Timestamp"] = DateTime.UtcNow
        };

        _logger.LogDebug("Generation cycle completed: {@Metrics}", metrics);

        // In Seq, create alerts:
        // - Metrics.TotalDuration_ms > 1000
        // - Metrics.Throughput_PerSecond < 50
    }
}
