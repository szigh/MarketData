using MarketData.Data;
using MarketData.Models;
using MarketData.PriceSimulator;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MarketData.Services;

/// <summary>
/// Manages price simulator models for instruments, including configuration validation,
/// default model assignment, simulator instantiation, and configuration CRUD operations.
/// This service handles all business logic for instrument models and can be used by
/// both HTTP controllers and gRPC services.
/// </summary>
public class InstrumentModelManager : IInstrumentModelManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPriceSimulatorFactory _simulatorFactory;
    private readonly ILogger<InstrumentModelManager> _logger;
    private readonly IDefaultModelConfigFactory _configFactory;
    private const string DefaultModelType = "Flat";

    public event EventHandler<ModelConfigurationChangedEventArgs>? ConfigurationChanged;
    public event EventHandler<ModelConfigurationChangedEventArgs>? ModelSwitched;
    public event EventHandler<ModelConfigurationChangedEventArgs>? TickIntervalChanged;
    public event EventHandler<ModelConfigurationChangedEventArgs>? InstrumentAdded;
    public event EventHandler<ModelConfigurationChangedEventArgs>? InstrumentRemoved;

    public InstrumentModelManager(
        IServiceProvider serviceProvider,
        IPriceSimulatorFactory simulatorFactory,
        ILogger<InstrumentModelManager> logger,
        IDefaultModelConfigFactory configFactory)
    {
        _serviceProvider = serviceProvider;
        _simulatorFactory = simulatorFactory;
        _logger = logger;
        _configFactory = configFactory;
    }

    protected virtual void OnConfigurationChanged(string instrumentName) => 
        ConfigurationChanged?.Invoke(this, new ModelConfigurationChangedEventArgs
    {
        InstrumentName = instrumentName
    });

    protected void OnModelSwitched(string instrumentName, string newModelType) => 
        ModelSwitched?.Invoke(this, new ModelConfigurationChangedEventArgs
    {
        InstrumentName = instrumentName,
        NewModelType = newModelType
    });

    protected void OnTickIntervalChanged(string instrumentName, int newTickIntervalMs) => 
        TickIntervalChanged?.Invoke(this, new ModelConfigurationChangedEventArgs
    {
        InstrumentName = instrumentName,
        NewTickIntervalMs = newTickIntervalMs
    });

    protected void OnInstrumentAdded(string instrumentName) => 
        InstrumentAdded?.Invoke(this, new ModelConfigurationChangedEventArgs
    {
        InstrumentName = instrumentName
    });

    protected void OnInstrumentRemoved(string instrumentName) => 
        InstrumentRemoved?.Invoke(this, new ModelConfigurationChangedEventArgs
    {
        InstrumentName = instrumentName
    });

    /// <summary>
    /// Used to seed the database with an initial instrument and price. 
    /// If the instrument already exists, it simply returns it.
    /// </summary>
    public async Task<(Instrument instrument, bool created)> GetOrCreateInstrumentAsync(
        string instrumentName, int tickIntervalMs,
        decimal initialPriceValue, DateTime initialPriceTimestamp, 
        string? modelType = null,
        CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();
        var instrument = await context.Instruments
            .Include(i => i.RandomMultiplicativeConfig)
            .Include(i => i.MeanRevertingConfig)
            .Include(i => i.FlatConfig)
            .Include(i => i.RandomAdditiveWalkConfig)
            .FirstOrDefaultAsync(i => i.Name == instrumentName, ct);
        if (instrument == null)
        {
            _logger.LogInformation("Creating new instrument: {InstrumentName}", instrumentName);
            instrument = new Instrument 
            { 
                Name = instrumentName, 
                TickIntervalMillieconds = tickIntervalMs 
            };

            if (modelType != null)
            {
                instrument.ModelType = modelType; 
                //validation is done below in EnsureModelTypeAsync,
                //  which will set to default if invalid
            }
            else
            {
                instrument.ModelType = DefaultModelType; // Set default model type if not provided
            }
            context.Instruments.Add(instrument);

            _logger.LogInformation("Adding initial price for instrument '{InstrumentName}': {Price} at {Timestamp}", 
                instrumentName, initialPriceValue, initialPriceTimestamp);
            var price = new Price
            {
                Instrument = instrumentName,
                Value = initialPriceValue,
                Timestamp = initialPriceTimestamp
            };
            context.Prices.Add(price);

            await context.SaveChangesAsync(ct);

            // Ensure default model type and configuration are set for the new instrument
            // This will also handle the case where an invalid model type was provided by setting it to default
            await EnsureModelTypeAsync(instrument, context, ct);

            // If the model type is valid, the *default* configuration will be created for that model type
            await EnsureModelConfigurationAsync(instrument, context, ct);

            OnInstrumentAdded(instrumentName); // Notify that a new instrument has been added

            return (instrument, true);
        }
        else
        {
            _logger.LogInformation("Instrument '{InstrumentName}' already exists", instrumentName);
            return (instrument, false);
        }
    }

    public async Task<bool> TryRemoveInstrumentAsync(string instrumentName, CancellationToken ct = default) 
    {         
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();
        var instrument = await context.Instruments
            .FirstOrDefaultAsync(i => i.Name == instrumentName, ct);
        if (instrument == null)
        {
            _logger.LogWarning("Attempted to remove non-existent instrument '{InstrumentName}'", instrumentName);
            return false;
        }
        context.Instruments.Remove(instrument);
        await context.SaveChangesAsync(ct);
        _logger.LogInformation("Removed instrument '{InstrumentName}'", instrumentName);
        OnInstrumentRemoved(instrumentName); // Notify that the instrument has been removed
        return true;
    }

    /// <summary>
    /// Ensures the instrument has a valid model type set. If not set, assigns the default model.
    /// </summary>
    /// <param name="instrument">The instrument to check and update</param>
    /// <param name="context">Database context to save changes</param>
    /// <returns>True if the model type was changed, false otherwise</returns>
    public async Task<bool> EnsureModelTypeAsync(
        Instrument instrument, 
        MarketDataContext context,
        CancellationToken ct = default)
    {
        var supportedModelTypes = GetSupportedModelTypes();
        if (string.IsNullOrWhiteSpace(instrument.ModelType) || !supportedModelTypes.Contains(instrument.ModelType))
        {
            _logger.LogWarning(
                "Instrument '{InstrumentName}' has no model type set or has an unsupported model type. " +
                "Setting to default: {DefaultModel}",
                instrument.Name, DefaultModelType);

            instrument.ModelType = DefaultModelType;
            await context.SaveChangesAsync(ct);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Validates that a model type is supported
    /// </summary>
    public static bool IsValidModelType(string modelType) => 
        GetSupportedModelTypes().Contains(modelType);

    /// <summary>
    /// Gets all supported model types
    /// </summary>
    public static string[] GetSupportedModelTypes() => 
        ["RandomMultiplicative", "MeanReverting", "Flat", "RandomAdditiveWalk"];

    /// <summary>
    /// Creates the appropriate price simulator for an instrument based on its model type and configuration
    /// </summary>
    public IPriceSimulator CreatePriceSimulator(Instrument instrument) =>
        _simulatorFactory.CreateSimulator(instrument);

    #region Configuration Management Methods

    /// <summary>
    /// Loads all instruments with configurations and ensures they are properly initialized.
    /// It is NOT readonly - if any instruments are missing configurations for their model type, they will be created with default values.
    /// This is an efficient batch operation that uses a single DbContext.
    /// </summary>
    public async Task<Dictionary<string, Instrument>> LoadAndInitializeAllInstrumentsAsync(
        CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        _logger.LogInformation("Loading all instruments with configurations from the database");

        var instruments = await context.Instruments
            .Include(i => i.RandomMultiplicativeConfig)
            .Include(i => i.MeanRevertingConfig)
            .Include(i => i.FlatConfig)
            .Include(i => i.RandomAdditiveWalkConfig)
            .ToListAsync(ct);
            
        // Ensure all instruments are properly configured using the same context
        foreach (var instrument in instruments)
        {
            await EnsureModelTypeAsync(instrument, context, ct);
            await EnsureModelConfigurationAsync(instrument, context, ct);
        }

        return instruments.ToDictionary(i => i.Name, i => i);
    }

    /// <summary>
    /// Ensures the instrument has a configuration for its current model type.
    /// Creates a default configuration if missing.
    /// </summary>
    public async Task EnsureModelConfigurationAsync(
        Instrument instrument, 
        MarketDataContext context,
        CancellationToken ct = default)
    {
        var configExists = instrument.ModelType switch
        {
            "RandomMultiplicative" => instrument.RandomMultiplicativeConfig != null,
            "MeanReverting" => instrument.MeanRevertingConfig != null,
            "Flat" => instrument.FlatConfig != null,
            "RandomAdditiveWalk" => instrument.RandomAdditiveWalkConfig != null,
            _ => false
        };

        if (!configExists)
        {
            _logger.LogWarning(
                "Instrument '{InstrumentName}' is set to model '{ModelType}' but has no configuration. Creating default configuration.",
                instrument.Name, instrument.ModelType);

            await CreateDefaultConfigurationAsync(instrument, context, ct);
        }
    }

    /// <summary>
    /// Gets an instrument with all its model configurations loaded
    /// </summary>
    public async Task<Instrument?> GetInstrumentWithConfigurationsAsync(
        string instrumentName, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        _logger.LogInformation("Retrieving instrument '{InstrumentName}' with configurations", instrumentName);

        return await context.Instruments
            .Include(i => i.RandomMultiplicativeConfig)
            .Include(i => i.MeanRevertingConfig)
            .Include(i => i.FlatConfig)
            .Include(i => i.RandomAdditiveWalkConfig)
            .FirstOrDefaultAsync(i => i.Name == instrumentName, ct);
    }


    /// <summary>
    /// Creates a default configuration for the instrument's current model type
    /// </summary>
    private async Task CreateDefaultConfigurationAsync(
        Instrument instrument, MarketDataContext context, CancellationToken ct = default)
    {
        switch (instrument.ModelType)
        {
            case "RandomMultiplicative":
                context.RandomMultiplicativeConfigs.Add(
                    _configFactory.CreateDefaultRandomMultiplicativeConfig(instrument.Id));
                break;

            case "MeanReverting":
                var lastPrice = await context.Prices
                    .Where(p => p.Instrument == instrument.Name)
                    .OrderByDescending(p => p.Timestamp)
                    .Select(p => p.Value)
                    .FirstOrDefaultAsync(ct);
                var mean = lastPrice == default ? 100d : (double)lastPrice; // Use last price as mean if available
                context.MeanRevertingConfigs.Add(
                    _configFactory.CreateMeanRevertingConfig(instrument.Id, mean));
                break;

            case "Flat":
                context.FlatConfigs.Add(_configFactory.CreateFlatConfig(instrument.Id));
                break;

            case "RandomAdditiveWalk":
                context.RandomAdditiveWalkConfigs.Add(
                    _configFactory.CreateRandomAdditiveWalkConfig(instrument.Id));
                break;
        }

        await context.SaveChangesAsync(ct);

        // Reload the navigation property
        await context.Entry(instrument).ReloadAsync(ct);
    }

    /// <summary>
    /// Switches the active model for an instrument.
    /// Automatically creates default configuration if it doesn't exist.
    /// </summary>
    /// <returns>The previous model type</returns>
    public async Task<string?> SwitchModelAsync(
        string instrumentName, string newModelType, CancellationToken ct = default)
    {
        if (!IsValidModelType(newModelType))
        {
            throw new ArgumentException(
                $"Invalid model type '{newModelType}'. Valid types are: {string.Join(", ", GetSupportedModelTypes())}",
                nameof(newModelType));
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instrument = await context.Instruments
            .Include(i => i.RandomMultiplicativeConfig)
            .Include(i => i.MeanRevertingConfig)
            .Include(i => i.FlatConfig)
            .Include(i => i.RandomAdditiveWalkConfig)
            .FirstOrDefaultAsync(i => i.Name == instrumentName, ct);

        if (instrument == null)
        {
            throw new InvalidOperationException($"Instrument '{instrumentName}' not found");
        }

        var previousModel = instrument.ModelType;
        instrument.ModelType = newModelType;

        // Ensure configuration exists for the new model type
        await EnsureModelConfigurationAsync(instrument, context, ct);

        await context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Model switched for instrument '{InstrumentName}' from '{PreviousModel}' to '{NewModel}'",
            instrumentName, previousModel, newModelType);

        // Notify subscribers of configuration change
        OnModelSwitched(instrumentName, newModelType);

        return previousModel;
    }

    /// <summary>
    /// Updates RandomMultiplicative configuration for an instrument
    /// </summary>
    public async Task<RandomMultiplicativeConfig> UpdateRandomMultiplicativeConfigAsync(
        string instrumentName,
        double standardDeviation,
        double mean,
        CancellationToken ct = default)
    {
        if (standardDeviation <= 0)
        {
            throw new ArgumentException("Standard deviation must be positive", nameof(standardDeviation));
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instrument = await context.Instruments
            .Include(i => i.RandomMultiplicativeConfig)
            .FirstOrDefaultAsync(i => i.Name == instrumentName, ct);

        if (instrument == null)
        {
            throw new InvalidOperationException($"Instrument '{instrumentName}' not found");
        }

        if (instrument.RandomMultiplicativeConfig == null)
        {
            // Create new configuration
            instrument.RandomMultiplicativeConfig = new RandomMultiplicativeConfig
            {
                InstrumentId = instrument.Id,
                StandardDeviation = standardDeviation,
                Mean = mean
            };
            context.RandomMultiplicativeConfigs.Add(instrument.RandomMultiplicativeConfig);
        }
        else
        {
            // Update existing
            instrument.RandomMultiplicativeConfig.StandardDeviation = standardDeviation;
            instrument.RandomMultiplicativeConfig.Mean = mean;
        }

        await context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "RandomMultiplicative config updated for '{InstrumentName}': StdDev={StdDev}, Mean={Mean}",
            instrumentName, standardDeviation, mean);

        // Notify subscribers of configuration change
        OnConfigurationChanged(instrumentName);

        return instrument.RandomMultiplicativeConfig;
    }

    /// <summary>
    /// Updates MeanReverting configuration for an instrument
    /// </summary>
    public async Task<MeanRevertingConfig> UpdateMeanRevertingConfigAsync(
        string instrumentName,
        double mean,
        double kappa,
        double sigma,
        double dt,
        CancellationToken ct = default)
    {
        if (kappa <= 0)
        {
            throw new ArgumentException("Kappa (mean reversion strength) must be positive", nameof(kappa));
        }
        if (sigma < 0)
        {
            throw new ArgumentException("Sigma (volatility) cannot be negative", nameof(sigma));
        }
        if (dt <= 0)
        {
            throw new ArgumentException("Dt (time step) must be positive", nameof(dt));
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instrument = await context.Instruments
            .Include(i => i.MeanRevertingConfig)
            .FirstOrDefaultAsync(i => i.Name == instrumentName, ct);

        if (instrument == null)
        {
            throw new InvalidOperationException($"Instrument '{instrumentName}' not found");
        }

        if (instrument.MeanRevertingConfig == null)
        {
            instrument.MeanRevertingConfig = new MeanRevertingConfig
            {
                InstrumentId = instrument.Id,
                Mean = mean,
                Kappa = kappa,
                Sigma = sigma,
                Dt = dt
            };
            context.MeanRevertingConfigs.Add(instrument.MeanRevertingConfig);
        }
        else
        {
            instrument.MeanRevertingConfig.Mean = mean;
            instrument.MeanRevertingConfig.Kappa = kappa;
            instrument.MeanRevertingConfig.Sigma = sigma;
            instrument.MeanRevertingConfig.Dt = dt;
        }

        await context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "MeanReverting config updated for '{InstrumentName}': Mean={Mean}, Kappa={Kappa}, Sigma={Sigma}, Dt={Dt}",
            instrumentName, mean, kappa, sigma, dt);

        // Notify subscribers of configuration change
        OnConfigurationChanged(instrumentName);

        return instrument.MeanRevertingConfig;
    }

    /// <summary>
    /// Updates RandomAdditiveWalk configuration for an instrument
    /// </summary>
    public async Task<RandomAdditiveWalkConfig> UpdateRandomAdditiveWalkConfigAsync(
        string instrumentName,
        string walkStepsJson,
        CancellationToken ct = default)
    {
        // Validate JSON can be deserialized
        List<RandomWalkStep> steps;
        try
        {
            steps = JsonSerializer.Deserialize<List<RandomWalkStep>>(walkStepsJson)
                ?? throw new ArgumentException("Walk steps JSON must represent a valid array", nameof(walkStepsJson));
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid walk steps JSON", nameof(walkStepsJson), ex);
        }

        if (steps.Count == 0)
        {
            throw new ArgumentException("Walk steps cannot be empty", nameof(walkStepsJson));
        }

        // Validate probability constraints by constructing RandomWalkSteps
        // This ensures invalid configs are rejected at write-time rather than when the simulator is constructed
        try
        {
            _ = new RandomWalkSteps(steps);
        }
        catch (ArgumentException ex)
        {
            // Re-throw validation errors from RandomWalkSteps with the correct parameter name
            throw new ArgumentException(ex.Message, nameof(walkStepsJson), ex);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instrument = await context.Instruments
            .Include(i => i.RandomAdditiveWalkConfig)
            .FirstOrDefaultAsync(i => i.Name == instrumentName, ct);

        if (instrument == null)
        {
            throw new InvalidOperationException($"Instrument '{instrumentName}' not found");
        }

        if (instrument.RandomAdditiveWalkConfig == null)
        {
            instrument.RandomAdditiveWalkConfig = new RandomAdditiveWalkConfig
            {
                InstrumentId = instrument.Id,
                WalkStepsJson = walkStepsJson
            };
            context.RandomAdditiveWalkConfigs.Add(instrument.RandomAdditiveWalkConfig);
        }
        else
        {
            instrument.RandomAdditiveWalkConfig.WalkStepsJson = walkStepsJson;
        }

        await context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "RandomAdditiveWalk config updated for '{InstrumentName}'",
            instrumentName);

        // Notify subscribers of configuration change
        OnConfigurationChanged(instrumentName);

        return instrument.RandomAdditiveWalkConfig;
    }

    public async Task<int> UpdateTickIntervalAsync(
        string instrumentName, int tickIntervalMs,
        CancellationToken ct = default)
    {
        if (tickIntervalMs <= 0)
        {
            throw new ArgumentException("Tick interval must be a positive integer", nameof(tickIntervalMs));
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instrument = await context.Instruments
            .FirstOrDefaultAsync(i => i.Name == instrumentName, ct);

        if (instrument == null)
        {
            throw new InvalidOperationException($"Instrument '{instrumentName}' not found");
        }

        _logger.LogInformation(
            "Updating tick interval for instrument '{InstrumentName}' from {PreviousInterval}ms to {NewInterval}ms",
            instrumentName, instrument.TickIntervalMillieconds, tickIntervalMs);

        instrument.TickIntervalMillieconds = tickIntervalMs;

        await context.SaveChangesAsync(ct);
        OnTickIntervalChanged(instrumentName, tickIntervalMs);

        return instrument.TickIntervalMillieconds;
    }

    #endregion
}

