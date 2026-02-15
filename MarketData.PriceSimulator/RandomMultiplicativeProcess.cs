namespace MarketData.PriceSimulator;

public class RandomMultiplicativeProcess : IPriceSimulator
{
    private readonly double _standardDeviation; //or volatility
    private readonly double _mean; //or drift

    /// <summary>
    /// Initializes a new instance of the RandomMultiplicativeProcess class using the specified standard deviation to
    /// control the variability of the process.
    /// </summary>
    /// <param name="standardDeviation">The standard deviation that determines the variability of the process. Must be a positive value.
    ///     Example: If _standardDeviation = 0.02 (2%):
 	///     ~68% of moves will be within ±2% of the current price
 	///     ~95% of moves will be within ±4% of the current price</param>
    /// <param name="mean">The mean or drift of the process. Default is 0, which means no drift.
    ///     upward drift (e.g., modeling a stock with expected growth),
    ///     change the mean from 0 to something like 0.0001 (0.01% average growth per step).</param>
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
