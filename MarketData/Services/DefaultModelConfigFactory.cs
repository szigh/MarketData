using MarketData.Models;
using System.Text.Json;

namespace MarketData.Services;

/// <summary>
/// Utility class to create default model configurations.
/// The values are deliberately arbitrary.
/// </summary>
internal static class DefaultModelConfigFactory
{
    public static FlatConfig CreateFlatConfig(int instrumentId)
    {
        return new FlatConfig
        {
            InstrumentId = instrumentId
        };
    }

    public static RandomMultiplicativeConfig CreateRandomMultiplicativeConfig(int instrumentId)
    {
        return new RandomMultiplicativeConfig
        {
            InstrumentId = instrumentId,
            StandardDeviation = 0.00388, // default: 99% within 1%
            Mean = 0.0
        };
    }

    public static MeanRevertingConfig CreateMeanRevertingConfig(int instrumentId, double mean = 100d)
    {
        return new MeanRevertingConfig
        {
            InstrumentId = instrumentId,
            Mean = mean,
            Kappa = 0.0004,
            Sigma = 0.5,
            Dt = 0.1
        };
    }

    public static RandomAdditiveWalkConfig CreateRandomAdditiveWalkConfig(int instrumentId)
    {
        var walkSteps = new[]
        {
            new { Probability = 0.25, Value = -0.01 },
            new { Probability = 0.25, Value = -0.005 },
            new { Probability = 0.25, Value = 0.005 },
            new { Probability = 0.25, Value = 0.01 }
        };

        return new RandomAdditiveWalkConfig
        {
            InstrumentId = instrumentId,
            WalkStepsJson = JsonSerializer.Serialize(walkSteps)
        };
    }
}
