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
        _modelManager.ModelSwitched += OnConfigurationChanged;
        _modelManager.InstrumentAdded += OnConfigurationChanged;
        _modelManager.InstrumentRemoved += OnInstrumentRemoved;
        _modelManager.TickIntervalChanged += OnTickIntervalChanged;
    }

    /// <summary>
    /// Initializes instruments and price simulators from database
    /// </summary>
    private async Task InitializeAsync(CancellationToken ct)
    {
        _logger.LogInformation("Initializing instruments and price simulators...");

        // Load and initialize all instruments efficiently in a single batch
        var instrumentDict = await _modelManager.LoadAndInitializeAllInstrumentsAsync(ct);

        foreach (var kvp in instrumentDict)
        {
            _instruments[kvp.Key] = kvp.Value;
        }

        // Load initial prices and create simulators for each instrument
        var instrumentNames = _instruments.Keys.ToList();
        var latestPrices = await GetLatestPricesAsync(instrumentNames, ct);

        foreach (var instrument in _instruments.Values)
        {
            _lastPrices[instrument.Name] = latestPrices[instrument.Name].Value;
            _priceSimulators[instrument.Name] = _modelManager.CreatePriceSimulator(instrument);
        }

        _logger.LogInformation("Initialized {Count} instruments", _instruments.Count);
    }

    private async Task<Dictionary<string, Price>> GetLatestPricesAsync(
        IEnumerable<string> instrumentNames, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var latestPrices = await dbContext.Prices
            .AsNoTracking()
            .Where(p => instrumentNames.Contains(p.Instrument))
            .GroupBy(p => p.Instrument)
            .Select(g => g.OrderByDescending(p => p.Timestamp).First())
            .ToListAsync(ct);

        var result = latestPrices.ToDictionary(p => p.Instrument!, p => p);

        // Check for missing instruments
        var missingInstruments = instrumentNames.Except(result.Keys);
        if (missingInstruments.Any())
        {
            throw new InvalidOperationException(
                $"No initial prices found for instruments: {string.Join(", ", missingInstruments)}. " +
                $"Please seed the database with initial prices.");
        }

        return result;
    }

    private void OnConfigurationChanged(object? sender, ModelConfigurationChangedEventArgs e)
    {
        _logger.LogInformation(
            "Configuration changed for instrument '{InstrumentName}'. Triggering hot reload...",
            e.InstrumentName);

        // Fire and forget - don't block the caller
        _ = HotReloadInstrumentAsync(e.InstrumentName);
    }

    private void OnTickIntervalChanged(object? sender, ModelConfigurationChangedEventArgs e)
    {
        if (_instruments.TryGetValue(e.InstrumentName, out var instrument))
        {
            instrument.TickIntervalMillieconds = e.NewTickIntervalMs;
        }
    }

    private void OnInstrumentRemoved(object? sender, ModelConfigurationChangedEventArgs e)
    {
        _logger.LogInformation("Instrument '{InstrumentName}' removed. Cleaning up resources...", e.InstrumentName);

        // Remove instrument from dictionaries
        _instruments.TryRemove(e.InstrumentName, out _);
        _priceSimulators.TryRemove(e.InstrumentName, out _);
        _lastPrices.TryRemove(e.InstrumentName, out _);
        _lastTickTimes.TryRemove(e.InstrumentName, out _);
        _lastDatabaseUpdates.TryRemove(e.InstrumentName, out _);
        _lastGrpcPublish.TryRemove(e.InstrumentName, out _);
    }

    /// <summary>
    /// Hot reloads a single instrument's simulator with new configuration
    /// </summary>
    private async Task HotReloadInstrumentAsync(string instrumentName, CancellationToken ct = default)
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
                .FirstOrDefaultAsync(i => i.Name == instrumentName, ct);

            if (instrument == null)
            {
                _logger.LogWarning(
                    "Instrument '{InstrumentName}' not found during hot reload attempt",
                    instrumentName);
                return;
            }

            // Create new price simulator with updated configuration
            var newSimulator = _modelManager.CreatePriceSimulator(instrument);

            _priceSimulators[instrumentName] = newSimulator;

            // This is needed in the case where a new instrument is added
            if (!_lastPrices.ContainsKey(instrumentName))
            {
                var latestPrices = await GetLatestPricesAsync([instrument.Name], ct);
                var latestPrice = latestPrices[instrument.Name].Value;
                _lastPrices[instrumentName] = latestPrice;
            }

            // This step must be done after simulator is created + price is set,
            // to avoid issue when instrument is added:
            //      price is generated before simulator and last price are set
            _instruments[instrumentName] = instrument;

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
                await GeneratePriceForInstrumentAsync(instrument.Name, ct);

                if (_instruments.ContainsKey(instrument.Name))
                {
                    _lastTickTimes[instrument.Name] = now;
                }
            }
        }
    }

    private async Task GeneratePriceForInstrumentAsync(
        string instrumentName, 
        CancellationToken ct)
    {
        if(!_lastPrices.TryGetValue(instrumentName, out var currentPrice) || 
            !_priceSimulators.TryGetValue(instrumentName, out var simulator))
            return;

        var newPrice = (decimal)(await simulator.GenerateNextPrice((double)currentPrice));
        _lastPrices.TryUpdate(instrumentName, newPrice, currentPrice);

        var price = new Price
        {
            Instrument = instrumentName,
            Value = newPrice,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogTrace("Generated price for {Instrument}: {Price} (previous: {PreviousPrice})",
            instrumentName, newPrice, currentPrice);

        await PersistPriceAsync(price, ct);
        await PublishPriceAsync(price, ct);
    }

    private async Task PublishPriceAsync(Price price, CancellationToken ct = default)
    {
        if (!TakeActionNeeded(_lastGrpcPublish, price.Instrument!, price.Timestamp, _options.GrpcPublishMilliseconds))
            return;
           
        _logger.LogDebug("[{Timestamp}] Publishing price for {Instrument}: {Price}", 
            price.Timestamp, price.Instrument, price.Value);

        await MarketDataGrpcService.BroadcastPrice(price.Instrument!, price.Value, price.Timestamp, ct);

        if (_instruments.ContainsKey(price.Instrument!))
        {
            _lastGrpcPublish[price.Instrument!] = price.Timestamp;
        }
    }

    private async Task PersistPriceAsync(Price price, CancellationToken ct = default)
    {
        if (!TakeActionNeeded(_lastDatabaseUpdates, price.Instrument!, price.Timestamp, _options.DatabasePersistenceMilliseconds))
            return;

        _logger.LogDebug("[{Timestamp}] Persisting price for {Instrument}: {Price}", 
            price.Timestamp, price.Instrument, price.Value);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();
        dbContext.Prices.Add(price);
        await dbContext.SaveChangesAsync(ct);

        if (_instruments.ContainsKey(price.Instrument!))
        {
            _lastDatabaseUpdates[price.Instrument!] = price.Timestamp;
        }
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
        _modelManager.ModelSwitched -= OnConfigurationChanged;
        _modelManager.InstrumentAdded -= OnConfigurationChanged;
        _modelManager.InstrumentRemoved -= OnInstrumentRemoved;
        _modelManager.TickIntervalChanged -= OnTickIntervalChanged;

        base.Dispose();
    }
}
