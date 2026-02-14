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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Market Data Generator Service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GeneratePricesAsync(stoppingToken);

                //!important the CheckIntervalMilliseconds is the minimum refresh
                // individual instruments can be configured in the database
                // e.g. TICKER1 every 1000ms, TICKER2 every 500ms
                await Task.Delay(TimeSpan.FromMilliseconds(_options.CheckIntervalMilliseconds), stoppingToken);
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

    private async Task GeneratePricesAsync(CancellationToken cancellationToken)
    {     
        var now = DateTime.UtcNow;

        foreach (var instrument in _instruments)
        {
            if (!_lastTickTimes.TryGetValue(instrument.Name, out var lastTickTime))
            {
                lastTickTime = DateTime.MinValue;
            }

            var timeSinceLastTick = now - lastTickTime;

            if (timeSinceLastTick.TotalMilliseconds >= instrument.TickIntervalMillieconds)
            {
                await GeneratePriceForInstrument(instrument.Name, cancellationToken);
                _lastTickTimes[instrument.Name] = now;
            }
        }
    }

    private async Task GeneratePriceForInstrument(
        string instrumentName, 
        CancellationToken cancellationToken)
    {
        var currentPrice = _lastPrices[instrumentName];
        var newPrice = GenerateNewPrice(currentPrice);

        var price = new Price
        {
            Instrument = instrumentName,
            Value = newPrice,
            Timestamp = DateTime.UtcNow
        };

        await PersistPriceAsync(price, cancellationToken);
        _lastPrices[instrumentName] = newPrice;

        // Broadcast price update via gRPC
        await MarketDataGrpcService.BroadcastPrice(instrumentName, newPrice, price.Timestamp);

        _logger.LogInformation("Generated price for {Instrument}: {Price} (previous: {PreviousPrice})",
            instrumentName, newPrice, currentPrice);
    }

    private async Task PersistPriceAsync(Price price, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();
        dbContext.Prices.Add(price);
        await dbContext.SaveChangesAsync(cancellationToken);
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
