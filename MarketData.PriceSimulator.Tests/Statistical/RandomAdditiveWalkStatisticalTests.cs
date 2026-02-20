namespace MarketData.PriceSimulator.Tests.Statistical;

/// <summary>
/// Statistical tests for <see cref="RandomAdditiveWalk"/> that validate probability distribution
/// and long-run behavior. These tests use large sample sizes to ensure statistical power
/// and minimize false positives (flakiness).
/// </summary>
/// <remarks>
/// These tests verify that:
/// 1. Step selection follows the specified probability distribution
/// 2. All configured steps can be selected
/// 3. Price changes accumulate correctly over many iterations
/// </remarks>
public class RandomAdditiveWalkStatisticalTests
{
    [StatisticalFact]
    public async Task GenerateNextPrice_FollowsSpecifiedProbabilityDistribution()
    {
        StatisticalTestGuard.EnsureEnabled();

        // Test: Over many samples, step selection should match specified probabilities
        
        const int numSamples = 10_000;
        var steps = new RandomWalkSteps(
        [
            new(0.5, 2.0),   // 50% chance of +2
            new(0.3, -1.0),  // 30% chance of -1
            new(0.2, 5.0)    // 20% chance of +5
        ]);
        var walk = new RandomAdditiveWalk(steps);

        var stepCounts = new Dictionary<double, int>
        {
            { 2.0, 0 },
            { -1.0, 0 },
            { 5.0, 0 }
        };

        var currentPrice = 100.0;
        for (int i = 0; i < numSamples; i++)
        {
            var nextPrice = await walk.GenerateNextPrice(currentPrice);
            var change = nextPrice - currentPrice;
            
            if (stepCounts.TryGetValue(change, out var value))
            {
                stepCounts[change] = ++value;
            }
            
            currentPrice = nextPrice;
        }

        // Calculate observed frequencies
        var freq2 = (double)stepCounts[2.0] / numSamples;
        var freqNeg1 = (double)stepCounts[-1.0] / numSamples;
        var freq5 = (double)stepCounts[5.0] / numSamples;

        // Expected: 0.5, 0.3, 0.2
        // Allow ±5% tolerance (with 10,000 samples, this gives >99.9% confidence)
        Assert.InRange(freq2, 0.45, 0.55);
        Assert.InRange(freqNeg1, 0.25, 0.35);
        Assert.InRange(freq5, 0.15, 0.25);
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_SelectsAllConfiguredSteps()
    {
        StatisticalTestGuard.EnsureEnabled();

        // Test: All steps should be selected at least once over many iterations
        
        const int numSamples = 1_000;
        var steps = new RandomWalkSteps(
        [
            new(0.4, 1.0),
            new(0.3, 2.0),
            new(0.2, 3.0),
            new(0.1, 4.0)
        ]);
        var walk = new RandomAdditiveWalk(steps);

        var observedSteps = new HashSet<double>();
        var currentPrice = 100.0;

        for (int i = 0; i < numSamples; i++)
        {
            var nextPrice = await walk.GenerateNextPrice(currentPrice);
            var change = nextPrice - currentPrice;
            observedSteps.Add(change);
            currentPrice = nextPrice;
        }

        // All four steps should have been selected at least once
        Assert.Contains(1.0, observedSteps);
        Assert.Contains(2.0, observedSteps);
        Assert.Contains(3.0, observedSteps);
        Assert.Contains(4.0, observedSteps);
        Assert.Equal(4, observedSteps.Count);
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_ProducesExpectedMeanChange()
    {
        StatisticalTestGuard.EnsureEnabled();

        // Test: Average change over many samples should match weighted expected value
        
        const int numSamples = 10_000;
        var steps = new RandomWalkSteps(
        [
            new(0.5, 2.0),
            new(0.5, -2.0)
        ]);
        var walk = new RandomAdditiveWalk(steps);

        var totalChange = 0.0;
        var currentPrice = 100.0;

        for (int i = 0; i < numSamples; i++)
        {
            var nextPrice = await walk.GenerateNextPrice(currentPrice);
            totalChange += (nextPrice - currentPrice);
            currentPrice = nextPrice;
        }

        var averageChange = totalChange / numSamples;
        
        // Expected value: 0.5 * 2.0 + 0.5 * (-2.0) = 0.0
        // With 10,000 samples, average should be very close to 0
        Assert.InRange(averageChange, -0.1, 0.1);
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_WithAsymmetricSteps_ProducesExpectedBias()
    {
        StatisticalTestGuard.EnsureEnabled();

        // Test: Asymmetric probabilities should produce expected directional bias
        
        const int numSamples = 10_000;
        var steps = new RandomWalkSteps(
        [
            new(0.7, 1.0),   // 70% chance of +1
            new(0.3, -1.0)   // 30% chance of -1
        ]);
        var walk = new RandomAdditiveWalk(steps);

        var totalChange = 0.0;
        var currentPrice = 100.0;

        for (int i = 0; i < numSamples; i++)
        {
            var nextPrice = await walk.GenerateNextPrice(currentPrice);
            totalChange += (nextPrice - currentPrice);
            currentPrice = nextPrice;
        }

        var averageChange = totalChange / numSamples;
        
        // Expected value: 0.7 * 1.0 + 0.3 * (-1.0) = 0.4
        Assert.InRange(averageChange, 0.35, 0.45);
    }

    [StatisticalTheory]
    [InlineData(0.5, 0.5)]
    [InlineData(0.7, 0.3)]
    [InlineData(0.9, 0.1)]
    public async Task GenerateNextPrice_RespectsProbabilityRatios(double prob1, double prob2)
    {
        StatisticalTestGuard.EnsureEnabled();

        // Test: Ratio of step selections should match ratio of probabilities
        
        const int numSamples = 5_000;
        var steps = new RandomWalkSteps(
        [
            new(prob1, 1.0),
            new(prob2, -1.0)
        ]);
        var walk = new RandomAdditiveWalk(steps);

        int countPositive = 0;
        int countNegative = 0;
        var currentPrice = 100.0;

        for (int i = 0; i < numSamples; i++)
        {
            var nextPrice = await walk.GenerateNextPrice(currentPrice);
            var change = nextPrice - currentPrice;
            
            if (change > 0) countPositive++;
            else countNegative++;
            
            currentPrice = nextPrice;
        }

        var observedRatio = (double)countPositive / countNegative;
        var expectedRatio = prob1 / prob2;

        // Allow ±15% tolerance on ratio
        Assert.InRange(observedRatio, expectedRatio * 0.85, expectedRatio * 1.15);
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_CumulativeChangeReflectsExpectedValue()
    {
        StatisticalTestGuard.EnsureEnabled();

        // Test: Cumulative price change over many steps should approach expected value
        
        const int numPaths = 1_000;
        const int stepsPerPath = 100;
        var steps = new RandomWalkSteps(
        [
            new(0.4, 3.0),
            new(0.3, 1.0),
            new(0.2, -1.0),
            new(0.1, -3.0)
        ]);

        var expectedValuePerStep = 0.4 * 3.0 + 0.3 * 1.0 + 0.2 * (-1.0) + 0.1 * (-3.0);
        // = 1.2 + 0.3 - 0.2 - 0.3 = 1.0

        var finalChanges = new List<double>();

        for (int path = 0; path < numPaths; path++)
        {
            var walk = new RandomAdditiveWalk(steps);
            var price = 100.0;
            
            for (int step = 0; step < stepsPerPath; step++)
            {
                price = await walk.GenerateNextPrice(price);
            }
            
            finalChanges.Add(price - 100.0);
        }

        var averageFinalChange = finalChanges.Average();
        var expectedTotalChange = expectedValuePerStep * stepsPerPath;
        
        // Expected: 1.0 * 100 = 100
        // Allow ±20% tolerance due to variance
        Assert.InRange(averageFinalChange, expectedTotalChange * 0.8, expectedTotalChange * 1.2);
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_WithEqualProbabilities_DistributesEvenly()
    {
        StatisticalTestGuard.EnsureEnabled();

        // Test: Equal probabilities should produce roughly equal frequencies
        
        const int numSamples = 8_000;
        var steps = new RandomWalkSteps(
        [
            new(0.25, 1.0),
            new(0.25, 2.0),
            new(0.25, 3.0),
            new(0.25, 4.0)
        ]);
        var walk = new RandomAdditiveWalk(steps);

        var stepCounts = new Dictionary<double, int>
        {
            { 1.0, 0 }, { 2.0, 0 }, { 3.0, 0 }, { 4.0, 0 }
        };

        var currentPrice = 100.0;
        for (int i = 0; i < numSamples; i++)
        {
            var nextPrice = await walk.GenerateNextPrice(currentPrice);
            var change = nextPrice - currentPrice;
            stepCounts[change]++;
            currentPrice = nextPrice;
        }

        // Each should be selected roughly 25% of the time (±5%)
        foreach (var count in stepCounts.Values)
        {
            var frequency = (double)count / numSamples;
            Assert.InRange(frequency, 0.20, 0.30);
        }
    }
}
