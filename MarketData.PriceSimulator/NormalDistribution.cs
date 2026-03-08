namespace MarketData.PriceSimulator;

internal static class NormalDistribution
{
    /// <summary>
    /// Generates a normally distributed random number using the Box-Muller transform.
    /// </summary>
    /// <param name="mean">The mean (μ) of the distribution.</param>
    /// <param name="standardDeviation">The standard deviation (σ) of the distribution.</param>
    /// <returns>A random value from the normal distribution N(μ, σ²).</returns>
    public static double Generate(double mean, double standardDeviation)
    {
        if (standardDeviation < 0)
        {
            throw new ArgumentException("Standard deviation must be non-negative", nameof(standardDeviation));
        }

        // Box-Muller transform to generate normally distributed random numbers
        var u1 = Random.Shared.NextDouble();
        var u2 = Random.Shared.NextDouble();

        var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

        return mean + standardDeviation * z;
    }
}
