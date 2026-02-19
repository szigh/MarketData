using MarketData.Models;
using System.Text.Json;

namespace MarketData.Data;

/// <summary>
/// Helper class to seed sample model configurations for instruments
/// </summary>
public static class SampleModelConfigurationSeeder
{
    public static void SeedConfigurations(MarketDataContext context)
    {
        // Example: Add configurations for all model types
        // You can customize these values or add configurations for your specific instruments

        var instruments = context.Instruments.ToList();

        foreach (var instrument in instruments)
        {
            // Add RandomMultiplicative configuration
            if (!context.RandomMultiplicativeConfigs.Any(c => c.InstrumentId == instrument.Id))
            {
                context.RandomMultiplicativeConfigs.Add(new RandomMultiplicativeConfig
                {
                    InstrumentId = instrument.Id,
                    StandardDeviation = 0.00388, // 99% of moves stay within 1% of current price
                    Mean = 0.0 // No drift
                });
            }

            // Add MeanReverting configuration
            if (!context.MeanRevertingConfigs.Any(c => c.InstrumentId == instrument.Id))
            {
                const double SECONDS_PER_YEAR = 252 * 6.5 * 3600; // 5,875,200

                context.MeanRevertingConfigs.Add(new MeanRevertingConfig
                {
                    InstrumentId = instrument.Id,
                    Mean = 1600,
                    Kappa = 200 / SECONDS_PER_YEAR,
                    Sigma = 0.5,
                    Dt = 0.1
                });
            }

            // Add Flat configuration
            if (!context.FlatConfigs.Any(c => c.InstrumentId == instrument.Id))
            {
                context.FlatConfigs.Add(new FlatConfig
                {
                    InstrumentId = instrument.Id
                });
            }

            // Add RandomAdditiveWalk configuration
            if (!context.RandomAdditiveWalkConfigs.Any(c => c.InstrumentId == instrument.Id))
            {
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
            }
        }

        context.SaveChanges();
    }
}
