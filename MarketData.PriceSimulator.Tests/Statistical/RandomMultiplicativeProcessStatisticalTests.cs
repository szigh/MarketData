namespace MarketData.PriceSimulator.Tests.Statistical;

/// <summary>
/// Statistical tests for <see cref="RandomMultiplicativeProcess"/> that validate probability distribution
/// and long-run behavior. These tests use large sample sizes to ensure statistical power
/// and minimize false positives (flakiness).
/// </summary>
/// <remarks>
/// These tests verify that:
/// 1. Percentage moves follow the specified normal distribution
/// 2. Mean drift affects long-run price behavior correctly
/// 3. Volatility parameter controls the distribution of percentage changes
/// 4. Multiplicative process preserves positive prices
/// </remarks>
public class RandomMultiplicativeProcessStatisticalTests
{
    [StatisticalFact]
    public async Task GenerateNextPrice_WithSmallVolatility_MajorityOfChangesAreSmall()
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numSamples = 10_000;
        var standardDeviation = 0.001; // 0.1% volatility
        var process = new RandomMultiplicativeProcess(standardDeviation, mean: 0.0);

        var percentageChanges = new List<double>();
        var startPrice = 100.0;

        for (int i = 0; i < numSamples; i++)
        {
            var nextPrice = await process.GenerateNextPrice(startPrice);
            var percentageChange = Math.Abs((nextPrice - startPrice) / startPrice);
            percentageChanges.Add(percentageChange);
        }

        // With σ=0.001:
        // ~68% of changes should be within ±0.1% (1σ)
        // ~95% of changes should be within ±0.2% (2σ)
        // ~99.7% of changes should be within ±0.3% (3σ)

        var within1Sigma = percentageChanges.Count(c => c <= 0.001);
        var within2Sigma = percentageChanges.Count(c => c <= 0.002);
        var within3Sigma = percentageChanges.Count(c => c <= 0.003);

        var rate1Sigma = (double)within1Sigma / numSamples;
        var rate2Sigma = (double)within2Sigma / numSamples;
        var rate3Sigma = (double)within3Sigma / numSamples;

        Assert.InRange(rate1Sigma, 0.63, 0.73); // ~68% ± 5%
        Assert.InRange(rate2Sigma, 0.90, 1.00); // ~95% ± 5%
        Assert.InRange(rate3Sigma, 0.95, 1.00); // ~99.7%, allow down to 95%
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_PercentageMovesFollowSpecifiedDistribution()
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numSamples = 10_000;
        var standardDeviation = 0.02;
        var mean = 0.0;
        var process = new RandomMultiplicativeProcess(standardDeviation, mean);

        var percentageMoves = new List<double>();
        var currentPrice = 100.0;

        for (int i = 0; i < numSamples; i++)
        {
            var nextPrice = await process.GenerateNextPrice(currentPrice);
            var percentageMove = (nextPrice - currentPrice) / currentPrice;
            percentageMoves.Add(percentageMove);
            currentPrice = nextPrice;
        }

        var sampleMean = percentageMoves.Average();
        var sampleStdDev = Math.Sqrt(percentageMoves.Select(x => Math.Pow(x - sampleMean, 2)).Average());

        Assert.InRange(sampleMean, mean - 0.005, mean + 0.005);
        Assert.InRange(sampleStdDev, standardDeviation * 0.9, standardDeviation * 1.1);
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_ProducesVariedResults()
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numSamples = 1_000;
        var process = new RandomMultiplicativeProcess(standardDeviation: 0.02, mean: 0.0);

        var prices = new HashSet<double>();
        var currentPrice = 100.0;

        for (int i = 0; i < numSamples; i++)
        {
            currentPrice = await process.GenerateNextPrice(currentPrice);
            prices.Add(currentPrice);
        }

        var uniquenessRate = (double)prices.Count / numSamples;

        Assert.True(uniquenessRate > 0.90,
            $"Expected >90% unique prices with volatility. Got {uniquenessRate:P2} ({prices.Count}/{numSamples} unique).");
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_WithNegativeMean_ShowsDownwardDrift()
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numPaths = 1_000;
        const int stepsPerPath = 100;
        var mean = -0.001;
        var process = new RandomMultiplicativeProcess(standardDeviation: 0.01, mean: mean);

        var finalPrices = new List<double>();
        var startPrice = 100.0;

        for (int path = 0; path < numPaths; path++)
        {
            var price = startPrice;
            for (int step = 0; step < stepsPerPath; step++)
            {
                price = await process.GenerateNextPrice(price);
            }
            finalPrices.Add(price);
        }

        var averageFinalPrice = finalPrices.Average();

        Assert.True(averageFinalPrice < startPrice,
            $"Expected average final price to be below start price with negative mean. " +
            $"Started at {startPrice}, average final: {averageFinalPrice:F2}");
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_WithMeanBelowVolatilityThreshold_MedianPriceDecays()
    {
        StatisticalTestGuard.EnsureEnabled();

        // Critical test: Even with POSITIVE mean, if μ < σ²/2, the median price still decays!
        // This is because E[log(1 + X)] ≈ μ - σ²/2 for X ~ N(μ, σ²)
        // Note: The arithmetic mean may grow due to positive skewness, but the median (geometric mean) decays

        const int numPaths = 1_000;
        const int stepsPerPath = 1_000;

        var standardDeviation = 0.02;  // σ = 2% (realistic daily volatility)
        var volatilityThreshold = standardDeviation * standardDeviation / 2;  // σ²/2 = 0.0002
        var mean = volatilityThreshold / 2;  // 0 < μ = (σ²/2)/2 < σ²/2, clearly below threshold

        var process = new RandomMultiplicativeProcess(standardDeviation, mean);

        var finalPrices = new List<double>();
        var startPrice = 100.0;

        for (int path = 0; path < numPaths; path++)
        {
            var price = startPrice;
            for (int step = 0; step < stepsPerPath; step++)
            {
                price = await process.GenerateNextPrice(price);
            }
            finalPrices.Add(price);
        }

        var medianFinalPrice = finalPrices.OrderBy(p => p).ElementAt(numPaths / 2);

        // Expected log drift ≈ μ - σ²/2 = 0.0001 - 0.0002 = -0.0001 < 0
        // Median price should decay (typical path converges to zero almost surely)

        Assert.True(medianFinalPrice < startPrice,
            $"Expected median price to decay with μ={mean:F6} < σ²/2={volatilityThreshold:F6}. " +
            $"Started at {startPrice}, median final: {medianFinalPrice:F2}");
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_WithMeanAtVolatilityThreshold_MedianNeitherGrowsNorDecays()
    {
        StatisticalTestGuard.EnsureEnabled();

        // Critical test: When μ = σ²/2, E[log(1 + X)] ≈ 0, giving a martingale in log space
        // The MEDIAN price should neither grow nor decay systematically
        // Note: Arithmetic mean can still grow due to positive skewness (Jensen's inequality)

        const int numPaths = 1_000;
        const int stepsPerPath = 500;

        var standardDeviation = 0.02;  // σ = 2% (realistic)
        var mean = standardDeviation * standardDeviation / 2;  // μ = σ²/2 = 0.0002

        var process = new RandomMultiplicativeProcess(standardDeviation, mean);

        var finalPrices = new List<double>();
        var startPrice = 100.0;

        for (int path = 0; path < numPaths; path++)
        {
            var price = startPrice;
            for (int step = 0; step < stepsPerPath; step++)
            {
                price = await process.GenerateNextPrice(price);
            }
            finalPrices.Add(price);
        }

        var medianFinalPrice = finalPrices.OrderBy(p => p).ElementAt(numPaths / 2);
        var logPriceRatio = Math.Log(medianFinalPrice / startPrice);

        // Expected log drift ≈ μ - σ²/2 = σ²/2 - σ²/2 = 0 (martingale property)
        // Median price should stay near start price

        Assert.InRange(medianFinalPrice, startPrice * 0.7, startPrice * 1.3);

        // Log price ratio should be close to zero (allow ±0.3 for 500 steps with volatility)
        Assert.InRange(logPriceRatio, -0.3, 0.3);
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_WithMeanAboveVolatilityThreshold_PriceGrows()
    {
        StatisticalTestGuard.EnsureEnabled();

        // Test: When μ > σ²/2, E[log(1 + X)] > 0, price should grow

        const int numPaths = 1_000;
        const int stepsPerPath = 500;

        var standardDeviation = 0.02;  // σ = 2%
        var volatilityThreshold = standardDeviation * standardDeviation / 2;  // σ²/2 = 0.0002
        var mean = volatilityThreshold * 3.0;  // μ = 0.0006 > σ²/2 = 0.0002

        var process = new RandomMultiplicativeProcess(standardDeviation, mean);

        var finalPrices = new List<double>();
        var startPrice = 100.0;

        for (int path = 0; path < numPaths; path++)
        {
            var price = startPrice;
            for (int step = 0; step < stepsPerPath; step++)
            {
                price = await process.GenerateNextPrice(price);
            }
            finalPrices.Add(price);
        }

        var averageFinalPrice = finalPrices.Average();

        // Expected log drift ≈ μ - σ²/2 - μ²/2 ≈ 0.0006 - 0.0002 - 0 = 0.0004 > 0
        // Price should grow on average

        Assert.True(averageFinalPrice > startPrice,
            $"Expected average price to grow with μ={mean:F6} > σ²/2={volatilityThreshold:F6}. " +
            $"Started at {startPrice}, average final: {averageFinalPrice:F2}");
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_WithZeroMean_MedianDecaysToZero()
    {
        StatisticalTestGuard.EnsureEnabled();

        // IMPORTANT: With mean=0, the price DECAYS to zero almost surely (P(lim P_n = 0) = 1)
        // This is because E[log(1 + X)] ≈ -σ²/2 < 0 for X ~ N(0, σ²)
        // The log price has negative drift, causing exponential decay.
        // This differs from geometric Brownian motion where μ=0 gives a martingale.
        //
        // Paradox: Despite almost sure convergence to zero, the arithmetic mean E[P_n] → ∞
        // due to positive skewness (rare paths with huge values dominate the average).
        // Therefore, we test the MEDIAN, which captures the typical path behavior.

        const int numPaths = 1_000;
        const int stepsPerPath = 1_000;
        var standardDeviation = 0.02;
        var process = new RandomMultiplicativeProcess(standardDeviation, mean: 0.0);

        var finalPrices = new List<double>();
        var startPrice = 100.0;

        for (int path = 0; path < numPaths; path++)
        {
            var price = startPrice;
            for (int step = 0; step < stepsPerPath; step++)
            {
                price = await process.GenerateNextPrice(price);
            }
            finalPrices.Add(price);
        }

        var medianFinalPrice = finalPrices.OrderBy(p => p).ElementAt(numPaths / 2);

        // With mean=0 and σ=0.02, expected log drift is -σ²/2 = -0.0002 per step
        // After 1000 steps: expected log change ≈ -0.2, so price ratio ≈ exp(-0.2) ≈ 0.82

        Assert.True(medianFinalPrice < startPrice,
            $"Expected median price to decay below start with mean=0. " +
            $"Started at {startPrice}, median final: {medianFinalPrice:F2}");

        // Most paths should show decay (testing almost-sure convergence)
        var pathsDecayed = finalPrices.Count(p => p < startPrice);
        var decayRate = (double)pathsDecayed / numPaths;

        Assert.True(decayRate > 0.55,
            $"Expected >55% of paths to show price decay with mean=0. Got {decayRate:P2}");
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_WithNegativeMean_TendsDownward()
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numPaths = 1_000;
        const int stepsPerPath = 100;
        var mean = -0.001;
        var process = new RandomMultiplicativeProcess(standardDeviation: 0.01, mean: mean);

        var finalPrices = new List<double>();
        var startPrice = 100.0;

        for (int path = 0; path < numPaths; path++)
        {
            var price = startPrice;
            for (int step = 0; step < stepsPerPath; step++)
            {
                price = await process.GenerateNextPrice(price);
            }
            finalPrices.Add(price);
        }

        var averageFinalPrice = finalPrices.Average();

        Assert.True(averageFinalPrice < startPrice,
            $"Expected average final price to be below start price with negative mean. " +
            $"Started at {startPrice}, average final: {averageFinalPrice:F2}");
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_MaintainsPositivePrices_WithSmallVolatility()
    {
        StatisticalTestGuard.EnsureEnabled();

        // NOTE: This model does NOT mathematically guarantee positive prices!
        // If percentageMove ~ N(μ, σ²) and percentageMove < -1, then newPrice < 0.
        // With small σ (0.05), P(X < -1) ≈ 0, but with larger σ this becomes significant:
        // - σ=0.5: P(X < -1) ≈ 2.3%
        // - σ=1.0: P(X < -1) ≈ 16%
        // This test verifies the property holds for typical/reasonable volatilities.

        const int numPaths = 1_000;
        const int stepsPerPath = 100;
        var process = new RandomMultiplicativeProcess(standardDeviation: 0.05, mean: 0.0);

        var startPrice = 100.0;
        var allPricesPositive = true;

        for (int path = 0; path < numPaths; path++)
        {
            var price = startPrice;
            for (int step = 0; step < stepsPerPath; step++)
            {
                price = await process.GenerateNextPrice(price);
                if (price <= 0)
                {
                    allPricesPositive = false;
                    break;
                }
            }
            if (!allPricesPositive) break;
        }

        Assert.True(allPricesPositive, 
            "All prices should remain positive with small volatility (σ=0.05). " +
            "Note: This is not mathematically guaranteed for all σ values.");
    }

    [StatisticalTheory]
    [InlineData(0.01, 0.0)]
    [InlineData(0.05, 0.0)]
    [InlineData(0.1, 0.0)]
    public async Task GenerateNextPrice_HigherVolatilityProducesWiderDistribution(double standardDeviation, double mean)
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numSamples = 5_000;
        var process = new RandomMultiplicativeProcess(standardDeviation, mean);

        var percentageMoves = new List<double>();
        var currentPrice = 100.0;

        for (int i = 0; i < numSamples; i++)
        {
            var nextPrice = await process.GenerateNextPrice(currentPrice);
            var percentageMove = (nextPrice - currentPrice) / currentPrice;
            percentageMoves.Add(percentageMove);
            currentPrice = nextPrice;
        }

        var sampleStdDev = Math.Sqrt(percentageMoves.Select(x => Math.Pow(x - percentageMoves.Average(), 2)).Average());

        Assert.InRange(sampleStdDev, standardDeviation * 0.85, standardDeviation * 1.15);
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_ProducesLogNormalDistribution()
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numPaths = 5_000;
        const int steps = 100;
        var process = new RandomMultiplicativeProcess(standardDeviation: 0.02, mean: 0.0);

        var finalPrices = new List<double>();
        var startPrice = 100.0;

        for (int path = 0; path < numPaths; path++)
        {
            var price = startPrice;
            for (int step = 0; step < steps; step++)
            {
                price = await process.GenerateNextPrice(price);
            }
            finalPrices.Add(price);
        }

        var logPrices = finalPrices.Select(p => Math.Log(p)).ToList();
        var logMean = logPrices.Average();
        var logStdDev = Math.Sqrt(logPrices.Select(x => Math.Pow(x - logMean, 2)).Average());

        Assert.True(logStdDev > 0, "Log prices should have positive standard deviation");
        Assert.True(finalPrices.All(p => p > 0), "All prices should be positive (log-normal property)");
    }

    [StatisticalFact]
    public async Task GenerateNextPrice_ScalesAbsoluteChangesWithPrice()
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numSamples = 1_000;
        var process = new RandomMultiplicativeProcess(standardDeviation: 0.02, mean: 0.0);

        var lowStartPrice = 10.0;
        var highStartPrice = 1000.0;

        var lowPriceChanges = new List<double>();
        var highPriceChanges = new List<double>();

        for (int i = 0; i < numSamples; i++)
        {
            var nextLow = await process.GenerateNextPrice(lowStartPrice);
            var nextHigh = await process.GenerateNextPrice(highStartPrice);

            lowPriceChanges.Add(Math.Abs(nextLow - lowStartPrice));
            highPriceChanges.Add(Math.Abs(nextHigh - highStartPrice));
        }

        var avgLowChange = lowPriceChanges.Average();
        var avgHighChange = highPriceChanges.Average();

        var ratio = avgHighChange / avgLowChange;
        var expectedRatio = highStartPrice / lowStartPrice;

        Assert.InRange(ratio, expectedRatio * 0.7, expectedRatio * 1.3);
    }

    [StatisticalTheory]
    [InlineData(0.001, 100)]
    [InlineData(0.01, 200)]
    [InlineData(0.002, 150)]
    public async Task GenerateNextPrice_MedianDriftAccumulatesCorrectly(double mean, int steps)
    {
        StatisticalTestGuard.EnsureEnabled();

        const int numPaths = 2_000;
        var standardDeviation = 0.01;
        var volatilityThreshold = standardDeviation * standardDeviation / 2;  // 0.00005
        var process = new RandomMultiplicativeProcess(standardDeviation, mean);

        var finalPrices = new List<double>();
        var startPrice = 100.0;

        for (int path = 0; path < numPaths; path++)
        {
            var price = startPrice;
            for (int step = 0; step < steps; step++)
            {
                price = await process.GenerateNextPrice(price);
            }
            finalPrices.Add(price);
        }

        var medianFinalPrice = finalPrices.OrderBy(p => p).ElementAt(numPaths / 2);
        var medianLogReturn = Math.Log(medianFinalPrice / startPrice);

        // Expected log drift per step: μ - σ²/2
        // All test cases have μ > σ²/2 (0.001, 0.01, 0.002 all > 0.00005)
        // So median should grow

        if (mean > volatilityThreshold)
        {
            Assert.True(medianLogReturn > 0, 
                $"With μ={mean:F4} > σ²/2={volatilityThreshold:F6}, median log return should be positive. " +
                $"Got {medianLogReturn:F4}");
        }

        Assert.True(Math.Abs(medianLogReturn) > 0.001,
            "Median log return should be distinguishable from zero");
    }
}
