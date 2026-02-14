using MarketData.Data;
using MarketData.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarketData.Services;

public class MarketDataGeneratorService(
    IServiceProvider _serviceProvider,
    ILogger<MarketDataGeneratorService> _logger,
    IOptions<MarketDataGeneratorOptions> _options) : BackgroundService
{
    private readonly MarketDataGeneratorOptions _options = _options.Value;
    private readonly Dictionary<string, DateTime> _lastTickTimes = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Market Data Generator Service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GeneratePricesAsync(stoppingToken);
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
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instruments = await dbContext.Instruments.ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var instrument in instruments)
        {
            if (!_lastTickTimes.TryGetValue(instrument.Name, out var lastTickTime))
            {
                lastTickTime = DateTime.MinValue;
            }

            var timeSinceLastTick = now - lastTickTime;

            if (timeSinceLastTick.TotalSeconds >= instrument.TickIntervalSeconds)
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
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var latestPrice = await dbContext.Prices
            .AsNoTracking()
            .Where(p => p.Instrument == instrumentName)
            .OrderByDescending(p => p.Timestamp)
            .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException(
                    $"No initial price found for instrument '{instrumentName}'. " +
                    $"Please seed the database with an initial price.");
        var newPrice = GenerateNewPrice(latestPrice.Value);

        var price = new Price
        {
            Instrument = instrumentName,
            Value = newPrice,
            Timestamp = DateTime.UtcNow
        };

        dbContext.Prices.Add(price);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Broadcast price update via gRPC
        await MarketDataGrpcService.BroadcastPrice(instrumentName, newPrice, price.Timestamp);

        _logger.LogInformation("Generated price for {Instrument}: {Price} (previous: {PreviousPrice})",
            instrumentName, newPrice, latestPrice.Value);
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
