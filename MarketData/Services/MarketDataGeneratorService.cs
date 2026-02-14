using MarketData.Data;
using MarketData.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarketData.Services;

public class MarketDataGeneratorService : BackgroundService
{
    private readonly MarketDataGeneratorOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarketDataGeneratorService> _logger;

    private readonly Dictionary<string, DateTime> _lastTickTimes = [];
    private readonly Dictionary<string, DateTime> _lastDatabaseUpdates = [];
    private readonly Dictionary<string, DateTime> _lastGrpcPublish = [];

    private readonly Dictionary<string, decimal> _lastPrices = [];
    private readonly List<Instrument> _instruments = [];

    public MarketDataGeneratorService(
        IServiceProvider serviceProvider,
        ILogger<MarketDataGeneratorService> logger,
        IOptions<MarketDataGeneratorOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();
        _instruments = dbContext.Instruments.ToList();

        // Load initial prices for each instrument
        foreach (var instrument in _instruments)
        {
            var latestPrice = dbContext.Prices
                .AsNoTracking()
                .Where(p => p.Instrument == instrument.Name)
                .OrderByDescending(p => p.Timestamp)
                .FirstOrDefault()
                    ?? throw new InvalidOperationException(
                        $"No initial price found for instrument '{instrument.Name}'. " +
                        $"Please seed the database with an initial price.");

            _lastPrices[instrument.Name] = latestPrice.Value;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Market Data Generator Service starting...");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                //!important the CheckIntervalMilliseconds is the minimum refresh
                // individual instruments can be configured in the database
                // e.g. TICKER1 every 1000ms, TICKER2 every 500ms
                // the actual refresh will be the greater of CheckIntervalMilliseconds
                //  and the value in the database
                //There is a separate _options.DatabasePersistenceMilliseconds to throttle DB persistence
                //  and _options.GrpcPublishMilliseconds to throttle gRPC stream
                //  -- again, the actual persistence/publishing will be the greater of the values
                await GeneratePricesAsync(ct);
                await Task.Delay(TimeSpan.FromMilliseconds(_options.CheckIntervalMilliseconds), ct);
            }
            catch (OperationCanceledException)
            {
                //service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating market data");
                throw;
            }
        }

        _logger.LogInformation("Market Data Generator Service stopping...");
    }

    private async Task GeneratePricesAsync(CancellationToken ct)
    {     
        var now = DateTime.UtcNow;

        foreach (var instrument in _instruments)
        {
            if (TakeActionNeeded(_lastTickTimes, instrument.Name, now, instrument.TickIntervalMillieconds))
            {
                await GeneratePriceForInstrument(instrument.Name, ct);
                _lastTickTimes[instrument.Name] = now;
            }
        }
    }

    private async Task GeneratePriceForInstrument(
        string instrumentName, 
        CancellationToken ct)
    {
        var currentPrice = _lastPrices[instrumentName];
        var newPrice = GenerateNewPrice(currentPrice);
        _lastPrices[instrumentName] = newPrice;

        var price = new Price
        {
            Instrument = instrumentName,
            Value = newPrice,
            Timestamp = DateTime.UtcNow
        };
#pragma warning disable CA1873 // Avoid potentially expensive logging
        _logger.LogInformation("Generated price for {Instrument}: {Price} (previous: {PreviousPrice})",
            instrumentName, newPrice, currentPrice);
#pragma warning restore CA1873 // Avoid potentially expensive logging

        await PersistPriceAsync(price, ct);
        await PublishPriceAsync(price);
    }

    private async Task PublishPriceAsync(Price price)
    {
        if (!TakeActionNeeded(_lastGrpcPublish, price.Instrument!, price.Timestamp, _options.GrpcPublishMilliseconds))
            return;
           
        await MarketDataGrpcService.BroadcastPrice(price.Instrument!, price.Value, price.Timestamp);
        _lastGrpcPublish[price.Instrument!] = price.Timestamp;
    }

    private async Task PersistPriceAsync(Price price, CancellationToken ct)
    {
        if (!TakeActionNeeded(_lastDatabaseUpdates, price.Instrument!, price.Timestamp, _options.DatabasePersistenceMilliseconds))
            return;

        _lastDatabaseUpdates[price.Instrument!] = price.Timestamp;

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();
        dbContext.Prices.Add(price);
        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// determines whether an action is needed to be taken (e.g. publish/persist) for a given instrument
    ///     given the "lastActions" timestamps, the timestamp now (or for the relevant price/action),
    ///     and how long we should wait between actions (e.g. only publish every 100ms)
    /// </summary>
    /// <param name="lastActions"></param>
    /// <param name="instrument"></param>
    /// <param name="now"></param>
    /// <param name="millisecondsBetweenActions"></param>
    /// <returns></returns>
    private static bool TakeActionNeeded(
        Dictionary<string, DateTime> lastActions,
        string instrument,
        DateTime now,
        int millisecondsBetweenActions)
    {
        if (!lastActions.TryGetValue(instrument, out var lastActionTime))
        {
            lastActionTime = DateTime.MinValue;
        }

        var timeSinceLastAction = now - lastActionTime;

        return timeSinceLastAction.TotalMilliseconds >= millisecondsBetweenActions;
    }

    private static decimal GenerateNewPrice(decimal currentPrice)
    {
        // 99% of moves stay within 1% of current price
        // 99% ≈ ±2.576 standard deviations
        // Therefore: σ = 0.01 / 2.576 ≈ 0.00388
        var standardDeviation = 0.00388;

        // Generate relative price change as a percentage
        var percentageMove = GenerateNormalDistribution(0, standardDeviation);
        var newPrice = currentPrice * (decimal)(1 + percentageMove);

        return newPrice;
    }

    private static double GenerateNormalDistribution(double mean, double standardDeviation)
    {
        // Box-Muller transform to generate normally distributed random numbers
        var u1 = Random.Shared.NextDouble();
        var u2 = Random.Shared.NextDouble();

        var z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

        return mean + standardDeviation * z0;
    }
}
