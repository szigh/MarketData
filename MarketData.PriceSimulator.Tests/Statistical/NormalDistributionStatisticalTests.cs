namespace MarketData.PriceSimulator.Tests.Statistical;

/// <summary>
/// Statistical tests for <see cref="NormalDistribution"/> that validate the Box-Muller transform
/// produces correct normal distribution properties. These tests use large sample sizes to ensure
/// statistical power and minimize false positives (flakiness).
/// </summary>
/// <remarks>
/// These tests verify that:
/// 1. Generated samples have the correct mean
/// 2. Generated samples have the correct standard deviation
/// 3. Distribution follows the 68-95-99.7 rule (empirical rule)
/// 4. Box-Muller transform implementation is correct
/// </remarks>
public class NormalDistributionStatisticalTests
{
    [StatisticalFact]
    public void Generate_FollowsEmpiricalRule_68Percent()
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numSamples = 10_000;
        var mean = 100.0;
        var stdDev = 15.0;

        var samples = new List<double>();
        for (int i = 0; i < numSamples; i++)
        {
            samples.Add(NormalDistribution.Generate(mean, stdDev));
        }

        var within1Sigma = samples.Count(x => Math.Abs(x - mean) <= stdDev);
        var percentage = (double)within1Sigma / numSamples;

        Assert.InRange(percentage, 0.63, 0.73);
    }

    [StatisticalFact]
    public void Generate_FollowsEmpiricalRule_95Percent()
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numSamples = 10_000;
        var mean = 100.0;
        var stdDev = 15.0;

        var samples = new List<double>();
        for (int i = 0; i < numSamples; i++)
        {
            samples.Add(NormalDistribution.Generate(mean, stdDev));
        }

        var within2Sigma = samples.Count(x => Math.Abs(x - mean) <= 2 * stdDev);
        var percentage = (double)within2Sigma / numSamples;

        Assert.InRange(percentage, 0.93, 0.97);
    }

    [StatisticalFact]
    public void Generate_FollowsEmpiricalRule_997Percent()
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numSamples = 10_000;
        var mean = 100.0;
        var stdDev = 15.0;

        var samples = new List<double>();
        for (int i = 0; i < numSamples; i++)
        {
            samples.Add(NormalDistribution.Generate(mean, stdDev));
        }

        var within3Sigma = samples.Count(x => Math.Abs(x - mean) <= 3 * stdDev);
        var percentage = (double)within3Sigma / numSamples;

        Assert.InRange(percentage, 0.995, 1.0);
    }

    [StatisticalTheory]
    [InlineData(0.0, 1.0)]
    [InlineData(50.0, 10.0)]
    [InlineData(-25.0, 5.0)]
    [InlineData(1000.0, 100.0)]
    public void Generate_WithDifferentParameters_ProducesCorrectMean(double expectedMean, double standardDeviation)
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numSamples = 5_000;

        var samples = new List<double>();
        for (int i = 0; i < numSamples; i++)
        {
            samples.Add(NormalDistribution.Generate(expectedMean, standardDeviation));
        }

        var sampleMean = samples.Average();

        var tolerance = standardDeviation * 0.1;
        Assert.InRange(sampleMean, expectedMean - tolerance, expectedMean + tolerance);
    }

    [StatisticalTheory]
    [InlineData(0.1)]
    [InlineData(1.0)]
    [InlineData(5.0)]
    [InlineData(20.0)]
    public void Generate_WithDifferentStandardDeviations_ProducesCorrectStandardDeviation(double expectedStdDev)
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numSamples = 5_000;
        var mean = 0.0;

        var samples = new List<double>();
        for (int i = 0; i < numSamples; i++)
        {
            samples.Add(NormalDistribution.Generate(mean, expectedStdDev));
        }

        var sampleMean = samples.Average();
        var sampleStdDev = Math.Sqrt(samples.Select(x => Math.Pow(x - sampleMean, 2)).Average());

        Assert.InRange(sampleStdDev, expectedStdDev * 0.9, expectedStdDev * 1.1);
    }

    [StatisticalFact]
    public void Generate_ProducesSymmetricDistribution()
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numSamples = 10_000;
        var mean = 0.0;
        var stdDev = 10.0;

        var samples = new List<double>();
        for (int i = 0; i < numSamples; i++)
        {
            samples.Add(NormalDistribution.Generate(mean, stdDev));
        }

        var above = samples.Count(x => x > mean);
        var below = samples.Count(x => x < mean);

        var ratio = (double)above / below;

        Assert.InRange(ratio, 0.9, 1.1);
    }

    [StatisticalFact]
    public void Generate_ProducesUniqueValues()
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numSamples = 1_000;
        var samples = new HashSet<double>();

        for (int i = 0; i < numSamples; i++)
        {
            samples.Add(NormalDistribution.Generate(mean: 0.0, standardDeviation: 1.0));
        }

        var uniquenessRate = (double)samples.Count / numSamples;

        Assert.True(uniquenessRate > 0.95,
            $"Expected >95% unique values from normal distribution. Got {uniquenessRate:P2} ({samples.Count}/{numSamples} unique).");
    }

    [StatisticalFact]
    public void Generate_LargeStandardDeviation_ProducesWiderDistribution()
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numSamples = 5_000;
        var mean = 0.0;
        var smallStdDev = 1.0;
        var largeStdDev = 10.0;

        var smallSamples = new List<double>();
        var largeSamples = new List<double>();

        for (int i = 0; i < numSamples; i++)
        {
            smallSamples.Add(NormalDistribution.Generate(mean, smallStdDev));
            largeSamples.Add(NormalDistribution.Generate(mean, largeStdDev));
        }

        var smallRange = smallSamples.Max() - smallSamples.Min();
        var largeRange = largeSamples.Max() - largeSamples.Min();

        Assert.True(largeRange > smallRange * 5,
            $"Expected larger standard deviation to produce wider range. " +
            $"Small range: {smallRange:F2}, Large range: {largeRange:F2}");
    }

    [StatisticalFact]
    public void Generate_WithZeroStandardDeviation_AllValuesEqualMean()
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numSamples = 1_000;
        var mean = 42.5;
        var samples = new List<double>();

        for (int i = 0; i < numSamples; i++)
        {
            samples.Add(NormalDistribution.Generate(mean, standardDeviation: 0.0));
        }

        Assert.True(samples.All(x => x == mean),
            $"Expected all values to equal mean ({mean}) with σ=0");
        Assert.Equal(0.0, samples.Max() - samples.Min());
    }

    [StatisticalFact]
    public void Generate_DistributionShape_ApproximatelyNormal()
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numSamples = 10_000;
        var mean = 50.0;
        var stdDev = 10.0;

        var samples = new List<double>();
        for (int i = 0; i < numSamples; i++)
        {
            samples.Add(NormalDistribution.Generate(mean, stdDev));
        }

        var bins = new int[10];
        var binWidth = 8.0 * stdDev / bins.Length;

        foreach (var sample in samples)
        {
            var binIndex = (int)Math.Floor((sample - (mean - 4 * stdDev)) / binWidth);
            if (binIndex >= 0 && binIndex < bins.Length)
            {
                bins[binIndex]++;
            }
        }

        var peakBinIndex = Array.IndexOf(bins, bins.Max());
        var middleBins = new[] { bins.Length / 2 - 1, bins.Length / 2, bins.Length / 2 + 1 };

        Assert.Contains(peakBinIndex, middleBins);
    }
}
