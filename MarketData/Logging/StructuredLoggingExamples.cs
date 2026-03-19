namespace MarketData.Services;

/// <summary>
/// Examples of logging structured/tabular data with Serilog
/// </summary>
public class StructuredLoggingExamples
{
    private readonly ILogger<StructuredLoggingExamples> _logger;

    public StructuredLoggingExamples(ILogger<StructuredLoggingExamples> logger)
    {
        _logger = logger;
    }

    // Example 1: Log a dictionary
    public void LogDictionary()
    {
        var stats = new Dictionary<string, object>
        {
            ["TotalInstruments"] = 150,
            ["ActiveInstruments"] = 142,
            ["PausedInstruments"] = 8,
            ["AveragePrice"] = 125.67,
            ["LastUpdate"] = DateTime.UtcNow
        };

        // Serilog will serialize this as structured data
        _logger.LogInformation("Market statistics: {@Stats}", stats);
        
        // In Seq, you can query: Stats.TotalInstruments > 100
    }

    // Example 2: Log multiple related properties (table-like)
    public void LogInstrumentSnapshot(string symbol, double price, int volume, double change)
    {
        _logger.LogInformation(
            "Instrument snapshot: {Symbol} | Price: {Price:C} | Volume: {Volume:N0} | Change: {Change:P2}",
            symbol, price, volume, change);
        
        // In Seq, searchable by Symbol, Price, Volume, Change
    }

    // Example 3: Log a collection of objects (table)
    public void LogInstrumentTable()
    {
        var instruments = new[]
        {
            new { Symbol = "AAPL", Price = 175.23, Volume = 1_234_567 },
            new { Symbol = "MSFT", Price = 372.91, Volume = 987_654 },
            new { Symbol = "GOOGL", Price = 141.80, Volume = 2_345_678 }
        };

        // Destructured logging (@) makes it queryable in Seq
        _logger.LogInformation("Instrument prices: {@Instruments}", instruments);
        
        // In Seq, can query: Instruments[?].Symbol = 'AAPL'
    }

    // Example 4: Log complex nested structure
    public void LogMarketDepth()
    {
        var marketDepth = new
        {
            Symbol = "AAPL",
            Timestamp = DateTime.UtcNow,
            Bids = new[]
            {
                new { Price = 175.20, Size = 100 },
                new { Price = 175.19, Size = 250 },
                new { Price = 175.18, Size = 500 }
            },
            Asks = new[]
            {
                new { Price = 175.21, Size = 150 },
                new { Price = 175.22, Size = 300 },
                new { Price = 175.23, Size = 450 }
            }
        };

        _logger.LogInformation("Market depth update for {Symbol}: {@MarketDepth}", 
            marketDepth.Symbol, marketDepth);
    }

    // Example 5: Log with custom table formatting (for console)
    public void LogFormattedTable()
    {
        var instruments = new[]
        {
            new { Symbol = "AAPL", Price = 175.23, Volume = 1_234_567, Change = 0.0215 },
            new { Symbol = "MSFT", Price = 372.91, Volume = 987_654, Change = -0.0087 },
            new { Symbol = "GOOGL", Price = 141.80, Volume = 2_345_678, Change = 0.0341 }
        };

        var table = string.Join("\n", new[]
        {
            "╔════════╦══════════╦═══════════╦══════════╗",
            "║ Symbol ║  Price   ║  Volume   ║  Change  ║",
            "╠════════╬══════════╬═══════════╬══════════╣",
            string.Join("\n", instruments.Select(i => 
                $"║ {i.Symbol,-6} ║ {i.Price,8:F2} ║ {i.Volume,9:N0} ║ {i.Change,7:P2} ║")),
            "╚════════╩══════════╩═══════════╩══════════╝"
        });

        // Logs formatted table to console (but also structured data to Seq)
        _logger.LogInformation("Instrument Summary:\n{Table}\n{@Instruments}", 
            table, instruments);
    }

    // Example 6: Performance metrics as dictionary
    public void LogPerformanceMetrics()
    {
        var metrics = new Dictionary<string, double>
        {
            ["PriceGeneration_ms"] = 12.5,
            ["DatabaseWrite_ms"] = 45.2,
            ["GrpcPublish_ms"] = 8.7,
            ["Total_ms"] = 66.4,
            ["ThroughputPerSec"] = 150.6
        };

        _logger.LogInformation("Performance metrics: {@Metrics}", metrics);
        
        // Create Seq dashboard with: Metrics.Total_ms > 100
    }

    // Example 7: Log exception with context dictionary
    public void LogErrorWithContext(Exception ex, string instrumentName, double price)
    {
        var context = new Dictionary<string, object>
        {
            ["InstrumentName"] = instrumentName,
            ["AttemptedPrice"] = price,
            ["CurrentTime"] = DateTime.UtcNow,
            ["Environment"] = Environment.MachineName
        };

        _logger.LogError(ex, "Failed to update instrument. Context: {@Context}", context);
    }

    // Example 8: Batch operation summary
    public void LogBatchOperationSummary()
    {
        var summary = new
        {
            Operation = "PriceUpdate",
            TotalItems = 500,
            Successful = 487,
            Failed = 13,
            Duration = TimeSpan.FromSeconds(2.5),
            Errors = new[]
            {
                new { Instrument = "AAPL", Reason = "Negative price" },
                new { Instrument = "MSFT", Reason = "Stale data" }
            }
        };

        _logger.LogWarning("Batch operation completed with errors: {@Summary}", summary);
    }
}
