namespace MarketData.PriceSimulator;

/// <summary>
/// Implements a simple multiplicative price process where percentage moves follow a normal distribution.
/// </summary>
/// <remarks>
/// <para><b>Model:</b> newPrice = currentPrice × (1 + X), where X ~ N(μ, σ²)</para>
/// 
/// <para><b>Important Limitation:</b> This model does NOT guarantee positive prices.</para>
/// <para>
/// Since percentageMove ~ N(μ, σ²), if percentageMove &lt; -1, then newPrice = currentPrice × (1 + percentageMove) &lt; 0.
/// </para>
/// <para>
/// <b>Probability of negative prices:</b>
/// <list type="bullet">
///   <item>σ = 0.02: P(negative) ≈ 0% (negligible)</item>
///   <item>σ = 0.1: P(negative) ≈ 0% (very rare)</item>
///   <item>σ = 0.5: P(negative) ≈ 2.3% (significant)</item>
///   <item>σ = 1.0: P(negative) ≈ 16% (high risk)</item>
/// </list>
/// </para>
/// 
/// <para><b>Critical Mathematical Property - The σ²/2 Threshold:</b></para>
/// <para>
/// The expected log price change is: <b>E[log(1 + X)] ≈ μ - σ²/2</b>
/// </para>
/// <para>
/// This creates a counter-intuitive behavior:
/// <list type="bullet">
///   <item><b>μ = 0:</b> Price DECAYS (not a martingale!) because -σ²/2 &lt; 0</item>
///   <item><b>0 &lt; μ &lt; σ²/2:</b> Price still DECAYS despite positive mean</item>
///   <item><b>μ = σ²/2:</b> Price neither grows nor decays (near-martingale)</item>
///   <item><b>μ &gt; σ²/2:</b> Price GROWS as expected</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Example:</b> With σ = 0.02 (2% daily volatility):
/// <list type="bullet">
///   <item>σ²/2 = 0.0002 (0.02% threshold)</item>
///   <item>μ = 0: Price decays ~0.02% per step</item>
///   <item>μ = 0.0001: Still decays (below threshold)</item>
///   <item>μ = 0.0002: Approximately neutral</item>
///   <item>μ = 0.0004: Grows ~0.02% per step</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Comparison to Geometric Brownian Motion (GBM):</b>
/// Unlike GBM (newPrice = currentPrice × exp(X)), this model:
/// <list type="bullet">
///   <item>Decays with μ = 0 (GBM is a martingale at μ = 0)</item>
///   <item>Can produce negative prices (GBM always positive)</item>
///   <item>Has simpler arithmetic (easier to understand)</item>
/// </list>
/// </para>
/// </remarks>
public class RandomMultiplicativeProcess : IPriceSimulator
{
    private readonly double _standardDeviation; //or volatility
    private readonly double _mean; //or drift

    /// <summary>
    /// Initializes a new instance of the RandomMultiplicativeProcess class using the specified standard deviation to
    /// control the variability of the process.
    /// </summary>
    /// <param name="standardDeviation">The standard deviation that determines the variability of the process. Must be a positive value.
    ///     Example: If standardDeviation = 0.02 (2%):
    ///     ~68% of moves will be within ±2% of the current price
    ///     ~95% of moves will be within ±4% of the current price</param>
    /// <param name="mean">The mean or drift of the percentage moves. Default is 0.
    ///     <para><b>Important:</b> The relationship between mean and price drift is NON-TRIVIAL!</para>
    ///     <para>
    ///     The expected log price change per step is approximately: <b>E[log(1 + X)] ≈ μ - σ²/2</b>
    ///     </para>
    ///     <para>This means:</para>
    ///     <list type="bullet">
    ///         <item><b>μ &lt; σ²/2:</b> Price DECAYS on average (even if μ > 0!)</item>
    ///         <item><b>μ = σ²/2:</b> Price neither grows nor decays (near-martingale)</item>
    ///         <item><b>μ &gt; σ²/2:</b> Price GROWS on average</item>
    ///     </list>
    ///     <para>
    ///     <b>Example:</b> With σ = 0.02 (2% volatility), the threshold is σ²/2 = 0.0002 (0.02%).
    ///     To achieve upward drift, need μ > 0.0002, not just μ > 0.
    ///     </para>
    ///     <para>
    ///     <b>Rule of thumb:</b> For upward drift, set μ > σ²/2. 
    ///     For example, μ = σ²/2 × 2 gives a modest positive drift.
    ///     </para>
    /// </param>
    public RandomMultiplicativeProcess(double standardDeviation, double mean = 0d)
    {
        if(standardDeviation <= 0)
        {
            throw new ArgumentException("Standard deviation must be a positive value.", 
                nameof(standardDeviation));
        }

        _standardDeviation = standardDeviation;
        _mean = mean;
    }

    public async Task<double> GenerateNextPrice(double currentPrice)
    {
        // Generate relative price change as a percentage
        var percentageMove = NormalDistribution.Generate(_mean, _standardDeviation);
        var newPrice = currentPrice * (1 + percentageMove);

        return newPrice;
    }
}
