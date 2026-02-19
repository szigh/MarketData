using MarketData.Models;
using MarketData.PriceSimulator;
using System.Text.Json;

namespace MarketData.Services;

/// <summary>
/// Factory for creating price simulator instances based on instrument configurations.
/// Handles the creation logic and validation for all supported simulator types.
/// </summary>
public class PriceSimulatorFactory : IPriceSimulatorFactory
{
    private readonly ILogger<PriceSimulatorFactory> _logger;

    public PriceSimulatorFactory(ILogger<PriceSimulatorFactory> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates the appropriate price simulator for an instrument based on its model type and configuration
    /// </summary>
    public IPriceSimulator CreateSimulator(Instrument instrument)
    {
        if (string.IsNullOrWhiteSpace(instrument.ModelType))
        {
            throw new InvalidOperationException(
                $"Instrument '{instrument.Name}' has no model type set.");
        }

        return instrument.ModelType switch
        {
            "RandomMultiplicative" => CreateRandomMultiplicativeSimulator(instrument),
            "MeanReverting" => CreateMeanRevertingSimulator(instrument),
            "Flat" => CreateFlatSimulator(instrument),
            "RandomAdditiveWalk" => CreateRandomAdditiveWalkSimulator(instrument),
            _ => throw new InvalidOperationException(
                $"Unknown model type '{instrument.ModelType}' for instrument '{instrument.Name}'. " +
                $"Valid types are: {string.Join(", ", InstrumentModelManager.GetSupportedModelTypes())}")
        };
    }

    private IPriceSimulator CreateRandomMultiplicativeSimulator(Instrument instrument)
    {
        var config = instrument.RandomMultiplicativeConfig
            ?? throw new InvalidOperationException(
                $"No RandomMultiplicativeConfig found for instrument '{instrument.Name}'.");

        _logger.LogDebug(
            "Creating RandomMultiplicativeProcess for '{InstrumentName}' with StdDev={StdDev}, Mean={Mean}",
            instrument.Name, config.StandardDeviation, config.Mean);

        return new RandomMultiplicativeProcess(config.StandardDeviation, config.Mean);
    }

    private IPriceSimulator CreateMeanRevertingSimulator(Instrument instrument)
    {
        var config = instrument.MeanRevertingConfig
            ?? throw new InvalidOperationException(
                $"No MeanRevertingConfig found for instrument '{instrument.Name}'.");

        _logger.LogDebug(
            "Creating MeanRevertingProcess for '{InstrumentName}' with Mean={Mean}, Kappa={Kappa}, Sigma={Sigma}, Dt={Dt}",
            instrument.Name, config.Mean, config.Kappa, config.Sigma, config.Dt);

        return new MeanRevertingProcess(config.Mean, config.Kappa, config.Sigma, config.Dt);
    }

    private IPriceSimulator CreateFlatSimulator(Instrument instrument)
    {
        _logger.LogDebug("Creating Flat simulator for '{InstrumentName}'", instrument.Name);
        return new Flat();
    }

    private IPriceSimulator CreateRandomAdditiveWalkSimulator(Instrument instrument)
    {
        var config = instrument.RandomAdditiveWalkConfig
            ?? throw new InvalidOperationException(
                $"No RandomAdditiveWalkConfig found for instrument '{instrument.Name}'.");

        var walkSteps = JsonSerializer.Deserialize<List<RandomWalkStep>>(config.WalkStepsJson)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize WalkStepsJson for instrument '{instrument.Name}'.");

        _logger.LogDebug(
            "Creating RandomAdditiveWalk for '{InstrumentName}' with {StepCount} steps",
            instrument.Name, walkSteps.Count);

        return new RandomAdditiveWalk(new RandomWalkSteps(walkSteps));
    }
}
