using System.Collections.Concurrent;
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
    private readonly IInstrumentModelManager _modelManager;

    private readonly ConcurrentDictionary<string, IPriceSimulator> _priceSimulators = new();

    private readonly ConcurrentDictionary<string, DateTime> _lastTickTimes = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastDatabaseUpdates = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastGrpcPublish = new();

    private readonly ConcurrentDictionary<string, decimal> _lastPrices = new();
    private readonly ConcurrentDictionary<string, Instrument> _instruments = new();

    public MarketDataGeneratorService(
        IInstrumentModelManager modelManager,
        IServiceProvider serviceProvider,
        ILogger<MarketDataGeneratorService> logger,
        IOptions<MarketDataGeneratorOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _modelManager = modelManager;
        _options = options.Value;

        // Subscribe to configuration changes for hot reload
        _modelManager.ConfigurationChanged += OnConfigurationChanged;
    }

    /// <summary>
    /// Initializes instruments and price simulators from database
    /// </summary>
    private async Task InitializeAsync(CancellationToken ct)
    {
        _logger.LogInformation("Initializing instruments and price simulators...");

        // Load and initialize all instruments efficiently in a single batch
        var instrumentDict = await _modelManager.LoadAndInitializeAllInstrumentsAsync();

        foreach (var kvp in instrumentDict)
        {
            _instruments[kvp.Key] = kvp.Value;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        // Load initial prices and create simulators for each instrument
        foreach (var instrument in _instruments.Values)
        {
            var latestPrice = await dbContext.Prices
                .AsNoTracking()
                .Where(p => p.Instrument == instrument.Name)
                .OrderByDescending(p => p.Timestamp)
                .FirstOrDefaultAsync(ct)
                    ?? throw new InvalidOperationException(
                        $"No initial price found for instrument '{instrument.Name}'. " +
                        $"Please seed the database with an initial price.");

            _lastPrices[instrument.Name] = latestPrice.Value;
            _priceSimulators[instrument.Name] = _modelManager.CreatePriceSimulator(instrument);
        }

        _logger.LogInformation("Initialized {Count} instruments", _instruments.Count);
    }

    /// <summary>
    /// Event handler for configuration changes - triggers hot reload
    /// </summary>
    private void OnConfigurationChanged(object? sender, ModelConfigurationChangedEventArgs e)
    {
        _logger.LogInformation(
            "Configuration changed for instrument '{InstrumentName}' (Model: {ModelType}). Triggering hot reload...",
            e.InstrumentName, e.ModelType ?? "config update");

        // Fire and forget - don't block the caller
        _ = HotReloadInstrumentAsync(e.InstrumentName);
    }

    /// <summary>
    /// Hot reloads a single instrument's simulator with new configuration
    /// </summary>
    private async Task HotReloadInstrumentAsync(string instrumentName)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

            // Reload instrument with updated configuration
            var instrument = await context.Instruments
                .Include(i => i.RandomMultiplicativeConfig)
                .Include(i => i.MeanRevertingConfig)
                .Include(i => i.FlatConfig)
                .Include(i => i.RandomAdditiveWalkConfig)
                .FirstOrDefaultAsync(i => i.Name == instrumentName);

            if (instrument == null)
            {
                _logger.LogWarning(
                    "Instrument '{InstrumentName}' not found during hot reload attempt",
                    instrumentName);
                return;
            }

            // Create new price simulator with updated configuration
            var newSimulator = _modelManager.CreatePriceSimulator(instrument);

            // Thread-safe updates using ConcurrentDictionary
            _instruments[instrumentName] = instrument;
            _priceSimulators[instrumentName] = newSimulator;

            _logger.LogInformation(
                "Successfully hot reloaded instrument '{InstrumentName}' with model '{ModelType}'",
                instrumentName, instrument.ModelType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error during hot reload for instrument '{InstrumentName}'",
                instrumentName);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Market Data Generator Service starting...");

        try
        {
            await InitializeAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Market Data Generator Service");
            throw;
        }

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

        foreach (var instrument in _instruments.Values)
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
        var simulator = _priceSimulators[instrumentName];
        var newPrice = (decimal)(await simulator.GenerateNextPrice((double)currentPrice));
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
        ConcurrentDictionary<string, DateTime> lastActions,
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

    public override void Dispose()
    {
        // Unsubscribe from configuration changes
        _modelManager.ConfigurationChanged -= OnConfigurationChanged;

        base.Dispose();
    }
}
