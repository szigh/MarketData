using MarketData.Data;
using MarketData.Models;
using MarketData.PriceSimulator;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MarketData.Services;

/// <summary>
/// Event args for configuration changes
/// </summary>
public class ModelConfigurationChangedEventArgs : EventArgs
{
    public string InstrumentName { get; init; } = string.Empty;
    public string? ModelType { get; init; }
    public DateTime Timestamp { get; init; }
}

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
    private const string DefaultModelType = "Flat";

    /// <summary>
    /// Event raised when a model configuration is changed.
    /// Subscribers can use this to hot-reload simulators.
    /// </summary>
    public event EventHandler<ModelConfigurationChangedEventArgs>? ConfigurationChanged;

    public InstrumentModelManager(
        IServiceProvider serviceProvider,
        IPriceSimulatorFactory simulatorFactory,
        ILogger<InstrumentModelManager> logger)
    {
        _serviceProvider = serviceProvider;
        _simulatorFactory = simulatorFactory;
        _logger = logger;
    }

    /// <summary>
    /// Raises the ConfigurationChanged event
    /// </summary>
    protected virtual void OnConfigurationChanged(string instrumentName, string? modelType = null)
    {
        ConfigurationChanged?.Invoke(this, new ModelConfigurationChangedEventArgs
        {
            InstrumentName = instrumentName,
            ModelType = modelType,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Ensures the instrument has a valid model type set. If not set, assigns the default model.
    /// </summary>
    /// <param name="instrument">The instrument to check and update</param>
    /// <param name="context">Database context to save changes</param>
    /// <returns>True if the model type was changed, false otherwise</returns>
    public async Task<bool> EnsureModelTypeAsync(Instrument instrument, MarketDataContext context)
    {
        if (string.IsNullOrWhiteSpace(instrument.ModelType))
        {
            _logger.LogWarning(
                "Instrument '{InstrumentName}' has no model type set. Setting to default: {DefaultModel}",
                instrument.Name, DefaultModelType);

            instrument.ModelType = DefaultModelType;
            await context.SaveChangesAsync();

            return true;
        }

        return false;
    }

    /// <summary>
    /// Ensures the instrument has a configuration for its current model type.
    /// Creates a default configuration if missing.
    /// </summary>
    public async Task EnsureModelConfigurationAsync(Instrument instrument, MarketDataContext context)
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

            await CreateDefaultConfigurationAsync(instrument, context);
        }
    }

    /// <summary>
    /// Creates a default configuration for the instrument's current model type
    /// </summary>
    private static async Task CreateDefaultConfigurationAsync(Instrument instrument, MarketDataContext context)
    {
        switch (instrument.ModelType)
        {
            case "RandomMultiplicative":
                context.RandomMultiplicativeConfigs.Add(new RandomMultiplicativeConfig
                {
                    InstrumentId = instrument.Id,
                    StandardDeviation = 0.00388, // Conservative default: 99% within 1%
                    Mean = 0.0
                });
                break;

            case "MeanReverting":
                const double SECONDS_PER_YEAR = 252 * 6.5 * 3600;
                var lastPrice = await context.Prices
                    .Where(p => p.Instrument == instrument.Name)
                    .OrderByDescending(p => p.Timestamp)
                    .Select(p => p.Value)
                    .FirstOrDefaultAsync();
                var mean = lastPrice == default ? 100m: lastPrice; // Use last price as mean if available
                context.MeanRevertingConfigs.Add(new MeanRevertingConfig
                {
                    InstrumentId = instrument.Id,
                    Mean = decimal.ToDouble(mean),
                    Kappa = 200 / SECONDS_PER_YEAR,
                    Sigma = 0.5,
                    Dt = 0.1
                });
                break;

            case "Flat":
                if (instrument.FlatConfig == null)
                {
                    context.FlatConfigs.Add(new FlatConfig
                    {
                        InstrumentId = instrument.Id
                    });
                }
                break;

            case "RandomAdditiveWalk":
                var walkSteps = new[]
                {
                    new { Probability = 0.25, Value = -0.01 },
                    new { Probability = 0.25, Value = -0.005 },
                    new { Probability = 0.25, Value = 0.005 },
                    new { Probability = 0.25, Value = 0.01 }
                };

                context.RandomAdditiveWalkConfigs.Add(new RandomAdditiveWalkConfig
                {
                    InstrumentId = instrument.Id,
                    WalkStepsJson = JsonSerializer.Serialize(walkSteps)
                });
                break;
        }

        await context.SaveChangesAsync();

        // Reload the navigation property
        await context.Entry(instrument).ReloadAsync();
    }

    /// <summary>
    /// Creates the appropriate price simulator for an instrument based on its model type and configuration
    /// </summary>
    public IPriceSimulator CreatePriceSimulator(Instrument instrument)
    {
        return _simulatorFactory.CreateSimulator(instrument);
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

    #region Configuration Management Methods

    /// <summary>
    /// Loads all instruments with configurations and ensures they are properly initialized.
    /// This is an efficient batch operation that uses a single DbContext.
    /// </summary>
    public async Task<Dictionary<string, Instrument>> LoadAndInitializeAllInstrumentsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instruments = await context.Instruments
            .Include(i => i.RandomMultiplicativeConfig)
            .Include(i => i.MeanRevertingConfig)
            .Include(i => i.FlatConfig)
            .Include(i => i.RandomAdditiveWalkConfig)
            .ToListAsync();

        // Ensure all instruments are properly configured using the same context
        foreach (var instrument in instruments)
        {
            await EnsureModelTypeAsync(instrument, context);
            await EnsureModelConfigurationAsync(instrument, context);
        }

        return instruments.ToDictionary(i => i.Name, i => i);
    }

    /// <summary>
    /// Gets an instrument with all its model configurations loaded
    /// </summary>
    public async Task<Instrument?> GetInstrumentWithConfigurationsAsync(string instrumentName)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        return await context.Instruments
            .Include(i => i.RandomMultiplicativeConfig)
            .Include(i => i.MeanRevertingConfig)
            .Include(i => i.FlatConfig)
            .Include(i => i.RandomAdditiveWalkConfig)
            .FirstOrDefaultAsync(i => i.Name == instrumentName);
    }

    /// <summary>
    /// Switches the active model for an instrument.
    /// Automatically creates default configuration if it doesn't exist.
    /// </summary>
    /// <returns>The previous model type</returns>
    public async Task<string?> SwitchModelAsync(string instrumentName, string newModelType)
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
            .FirstOrDefaultAsync(i => i.Name == instrumentName);

        if (instrument == null)
        {
            throw new InvalidOperationException($"Instrument '{instrumentName}' not found");
        }

        var previousModel = instrument.ModelType;
        instrument.ModelType = newModelType;

        // Ensure configuration exists for the new model type
        await EnsureModelConfigurationAsync(instrument, context);

        await context.SaveChangesAsync();

        _logger.LogInformation(
            "Model switched for instrument '{InstrumentName}' from '{PreviousModel}' to '{NewModel}'",
            instrumentName, previousModel, newModelType);

        // Notify subscribers of configuration change
        OnConfigurationChanged(instrumentName, newModelType);

        return previousModel;
    }

    /// <summary>
    /// Updates RandomMultiplicative configuration for an instrument
    /// </summary>
    public async Task<RandomMultiplicativeConfig> UpdateRandomMultiplicativeConfigAsync(
        string instrumentName,
        double standardDeviation,
        double mean)
    {
        if (standardDeviation <= 0)
        {
            throw new ArgumentException("Standard deviation must be positive", nameof(standardDeviation));
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instrument = await context.Instruments
            .Include(i => i.RandomMultiplicativeConfig)
            .FirstOrDefaultAsync(i => i.Name == instrumentName);

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

        await context.SaveChangesAsync();

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
        double dt)
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
            .FirstOrDefaultAsync(i => i.Name == instrumentName);

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

        await context.SaveChangesAsync();

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
        string walkStepsJson)
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
            .FirstOrDefaultAsync(i => i.Name == instrumentName);

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

        await context.SaveChangesAsync();

        _logger.LogInformation(
            "RandomAdditiveWalk config updated for '{InstrumentName}'",
            instrumentName);

        // Notify subscribers of configuration change
        OnConfigurationChanged(instrumentName);

        return instrument.RandomAdditiveWalkConfig;
    }

    public async Task<int> UpdateTickIntervalAsync(string instrumentName, int tickIntervalMs)
    {
        if (tickIntervalMs <= 0)
        {
            throw new ArgumentException("Tick interval must be a positive integer", nameof(tickIntervalMs));
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instrument = await context.Instruments
            .FirstOrDefaultAsync(i => i.Name == instrumentName);

        if (instrument == null)
        {
            throw new InvalidOperationException($"Instrument '{instrumentName}' not found");
        }

        instrument.TickIntervalMillieconds = tickIntervalMs;

        await context.SaveChangesAsync();
        OnConfigurationChanged(instrumentName);

        return instrument.TickIntervalMillieconds;
    }

    #endregion
}

