using MarketData.Data;
using MarketData.Models;
using MarketData.PriceSimulator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarketData.Services;

public class MarketDataGeneratorService : BackgroundService
{
    private readonly MarketDataGeneratorOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarketDataGeneratorService> _logger;

    private readonly IPriceSimulator _priceSimulator;

    private readonly Dictionary<string, DateTime> _lastTickTimes = [];
    private readonly Dictionary<string, decimal> _lastPrices = [];
    private readonly List<Instrument> _instruments = [];

    public MarketDataGeneratorService(
        IPriceSimulator priceSimulator,
        IServiceProvider serviceProvider,
        ILogger<MarketDataGeneratorService> logger,
        IOptions<MarketDataGeneratorOptions> options)
    {
        _priceSimulator = priceSimulator;
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
        var newPrice = (decimal)(await _priceSimulator.GenerateNextPrice((double)currentPrice));

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


}
