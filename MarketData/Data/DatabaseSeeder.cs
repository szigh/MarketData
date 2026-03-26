using MarketData.Models;
using Serilog;

namespace MarketData.Data;

internal static class DatabaseSeeder
{
    internal static void Seed(MarketDataContext context)
    {
        if (!context.Instruments.Any(i => i.Name == "FTSE"))
        {
            context.Instruments.Add(
                new Instrument
                {
                    Name = "FTSE",
                    TickIntervalMillieconds = 1000,
                    FlatConfig = new FlatConfig(),
                    MeanRevertingConfig = new MeanRevertingConfig
                    {
                        Mean = 10_000,
                        Kappa = 0.0005,
                        Sigma = 0.75,
                        Dt = 0.1
                    },
                    ModelType = ModelType.MeanReverting.ToString()
                }
            );
            context.Prices.Add(new Price
            {
                Instrument = "FTSE",
                Value = 10_000,
                Timestamp = DateTime.UtcNow
            });

            context.SaveChanges();
            Log.Information("Seeded instrument: FTSE");
        }
        else
        {
            Log.Information("Instrument FTSE already exists, skipping seeding");
        }
    }
}
