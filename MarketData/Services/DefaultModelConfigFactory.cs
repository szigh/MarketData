using MarketData.Models;
using System.Text.Json;

namespace MarketData.Services;

/// <summary>
/// Interface for creating default model configurations
/// </summary>
public interface IDefaultModelConfigFactory
{
    FlatConfig CreateFlatConfig(int instrumentId);
    RandomMultiplicativeConfig CreateDefaultRandomMultiplicativeConfig(int instrumentId);
    MeanRevertingConfig CreateMeanRevertingConfig(int instrumentId, double mean = 100d);
    RandomAdditiveWalkConfig CreateRandomAdditiveWalkConfig(int instrumentId);
}

/// <summary>
/// Factory for creating default model configurations.
/// The values are deliberately arbitrary.
/// </summary>
public class DefaultModelConfigFactory : IDefaultModelConfigFactory
{
    private readonly ILogger<DefaultModelConfigFactory> _logger;

    public DefaultModelConfigFactory(ILogger<DefaultModelConfigFactory> logger)
    {
        _logger = logger;
    }

    public FlatConfig CreateFlatConfig(int instrumentId)
    {
        _logger.LogDebug("Creating FlatConfig for instrument {InstrumentId}", instrumentId);

        return new FlatConfig
        {
            InstrumentId = instrumentId
        };
    }

    /// <summary>
    /// Utility method to create a RandomMultiplicativeConfig with arbitrary numbers
    /// </summary>
    public RandomMultiplicativeConfig CreateDefaultRandomMultiplicativeConfig(int instrumentId)
    {
        var config = new RandomMultiplicativeConfig
        {
            InstrumentId = instrumentId,
            StandardDeviation = 0.00388, // default: 99% within 1%
            Mean = 0.0
        };

        _logger.LogDebug("RandomMultiplicativeConfig created for instrument {InstrumentId}: {@Config}", 
            instrumentId, config);

        return config;
    }

    /// <summary>
    /// Utility method to create a MeanRevertingConfig with arbitrary numbers
    /// </summary>
    public MeanRevertingConfig CreateMeanRevertingConfig(int instrumentId, double mean = 100d)
    {
        var config = new MeanRevertingConfig
        {
            InstrumentId = instrumentId,
            Mean = mean,
            Kappa = 0.0004,
            Sigma = 0.5,
            Dt = 0.1
        };


        _logger.LogDebug("MeanRevertingConfig created for instrument {InstrumentId}: {@Config}",
            instrumentId, config);

        return config;
    }

    /// <summary>
    /// Utility method to create a RandomAdditiveWalkConfig with arbitrary numbers.
    /// </summary>
    public RandomAdditiveWalkConfig CreateRandomAdditiveWalkConfig(int instrumentId)
    {
        var walkSteps = new[]
        {
            new { Probability = 0.25, Value = -0.01 },
            new { Probability = 0.25, Value = -0.005 },
            new { Probability = 0.25, Value = 0.005 },
            new { Probability = 0.25, Value = 0.01 }
        };

        var config = new RandomAdditiveWalkConfig
        {
            InstrumentId = instrumentId,
            WalkStepsJson = JsonSerializer.Serialize(walkSteps)
        };

        _logger.LogDebug("RandomAdditiveWalkConfig created for instrument {InstrumentId}: {@Config}", 
            instrumentId, config);

        return config;
    }
}
