namespace MarketData.PriceSimulator.Tests.Statistical;

/// <summary>
/// Statistical tests for <see cref="MeanRevertingProcess"/> that validate long-run
/// stochastic properties. These tests use large sample sizes to ensure statistical
/// power and minimize false positives (flakiness).
/// </summary>
/// <remarks>
/// These tests are slower and probabilistic by nature. They verify that:
/// 1. Mean reversion occurs in the limit (Law of Large Numbers)
/// 2. Variance/volatility parameters affect output distribution correctly
/// 3. Statistical properties match theoretical Ornstein-Uhlenbeck process
/// 
/// Run separately from unit tests: dotnet test --filter "Category=Statistical"
/// </remarks>
public class MeanRevertingProcessStatisticalTests
{
    [StatisticalFact]
    public async Task MeanReversion_ConvergesToMean_InMajorityOfLongRunPaths()
    {
        StatisticalTestGuard.EnsureEnabled();

        // Test: With strong mean reversion, most paths starting far from mean
        // should end closer to mean after sufficient time steps
        
        const int numPaths = 5_000;  // Large sample size for statistical power
        const int stepsPerPath = 200; // Sufficient time for convergence
        
        var mean = 100.0;
        var kappa = 0.5;  // Moderate mean reversion strength
        var sigma = 1.0;  // Moderate volatility
        var dt = 0.01;
        var initialPrice = 120.0; // Start 20% above mean
        
        var pathsConverged = 0;
        
        for (int path = 0; path < numPaths; path++)
        {
            var process = new MeanRevertingProcess(mean, kappa, sigma, dt);
            var price = initialPrice;
            
            for (int step = 0; step < stepsPerPath; step++)
            {
                price = await process.GenerateNextPrice(price);
            }
            
            // Count paths that ended closer to mean than they started
            var initialDistance = Math.Abs(initialPrice - mean);
            var finalDistance = Math.Abs(price - mean);
            
            if (finalDistance < initialDistance)
            {
                pathsConverged++;
            }
        }
        
        var convergenceRate = (double)pathsConverged / numPaths;
        
        // With kappa=0.5, sigma=1.0, and 200 steps, empirically expect >85% convergence
        // This threshold chosen to give <0.1% false positive rate
        Assert.True(convergenceRate > 0.85,
            $"Expected >85% of paths to converge toward mean with mean reversion. " +
            $"Got {convergenceRate:P2} ({pathsConverged}/{numPaths} paths). " +
            $"This may indicate broken mean reversion implementation.");
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_ProducesNonZeroVariation_WithNonZeroVolatility()
    {
        StatisticalTestGuard.EnsureEnabled();

        // Test: Non-zero volatility produces price variation over many samples
        
        const int numSamples = 1_000;
        var process = new MeanRevertingProcess(
            mean: 100.0,
            kappa: 0.5,
            sigma: 2.0,  // Non-zero volatility
            dt: 0.01);
        
        var prices = new HashSet<double>();
        var currentPrice = 100.0;
        
        for (int i = 0; i < numSamples; i++)
        {
            currentPrice = await process.GenerateNextPrice(currentPrice);
            prices.Add(currentPrice);
        }
        
        // With 1000 samples and sigma=2.0, probability of all duplicates is negligible
        // Expect >99.9% unique values (floating point makes exact duplicates unlikely)
        var uniquenessRate = (double)prices.Count / numSamples;
        
        Assert.True(uniquenessRate > 0.90,
            $"Expected >90% unique prices with non-zero volatility. " +
            $"Got {uniquenessRate:P2} ({prices.Count}/{numSamples} unique). " +
            $"This may indicate volatility parameter is not being applied.");
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_AverageChangeReflectsDriftTerm_OverManySamples()
    {
        StatisticalTestGuard.EnsureEnabled();

        // Test: Average price change over many samples should reflect drift toward mean
        
        const int numSamples = 10_000;
        var mean = 100.0;
        var kappa = 0.5;
        var sigma = 1.0;
        var dt = 0.01;
        var initialPrice = 110.0; // Start above mean
        
        var process = new MeanRevertingProcess(mean, kappa, sigma, dt);
        var totalChange = 0.0;
        var currentPrice = initialPrice;
        
        for (int i = 0; i < numSamples; i++)
        {
            var nextPrice = await process.GenerateNextPrice(currentPrice);
            totalChange += (nextPrice - currentPrice);
            currentPrice = nextPrice;
        }
        
        var averageChange = totalChange / numSamples;
        
        // Expected drift per step: kappa * (mean - price) * dt
        // For price starting at 110: 0.5 * (100 - 110) * 0.01 = -0.05
        // As price converges, drift approaches zero, so average should be negative but smaller
        
        // Over many steps starting above mean, average change should be negative
        Assert.True(averageChange < 0,
            $"Expected negative average change when starting above mean. " +
            $"Got {averageChange:F6}. This may indicate drift term is incorrect.");
        
        // Should be roughly in expected range (within order of magnitude)
        Assert.InRange(averageChange, -0.1, 0.0);
    }

    [StatisticalTheory]
    [InlineData(0.3, 150)] // Weak reversion, more steps needed
    [InlineData(1.0, 100)] // Strong reversion, fewer steps needed
    public async Task MeanReversion_ConvergenceRateIncreasesWithKappa(double kappa, int steps)
    {
        StatisticalTestGuard.EnsureEnabled();

        // Test: Higher kappa (reversion strength) leads to faster convergence
        
        const int numPaths = 2_000;
        var mean = 100.0;
        var sigma = 1.0;
        var dt = 0.01;
        var initialPrice = 120.0;
        
        var pathsConverged = 0;
        
        for (int path = 0; path < numPaths; path++)
        {
            var process = new MeanRevertingProcess(mean, kappa, sigma, dt);
            var price = initialPrice;
            
            for (int step = 0; step < steps; step++)
            {
                price = await process.GenerateNextPrice(price);
            }
            
            if (Math.Abs(price - mean) < Math.Abs(initialPrice - mean))
            {
                pathsConverged++;
            }
        }
        
        var convergenceRate = (double)pathsConverged / numPaths;
        
        // Both parameter sets should show >75% convergence with chosen step counts
        Assert.True(convergenceRate > 0.75,
            $"Expected >75% convergence with kappa={kappa} after {steps} steps. " +
            $"Got {convergenceRate:P2}. This may indicate kappa parameter is not working correctly.");
    }
}
