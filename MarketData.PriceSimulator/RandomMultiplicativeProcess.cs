namespace MarketData.PriceSimulator;

public class RandomMultiplicativeProcess : IPriceSimulator
{
    private readonly double _standardDeviation;

    public RandomMultiplicativeProcess(double standardDeviation)
    {
        _standardDeviation = standardDeviation;
    }

    public async Task<double> GenerateNextPrice(double currentPrice)
    {
        // Generate relative price change as a percentage
        var percentageMove = GenerateNormalDistribution(0, _standardDeviation);
        var newPrice = currentPrice * (1 + percentageMove);

        return newPrice;
    }

    private static double GenerateNormalDistribution(double mean, double standardDeviation)
    {
        // Box-Muller transform to generate normally distributed random numbers
        var u1 = Random.Shared.NextDouble();
        var u2 = Random.Shared.NextDouble();

        var z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

        return mean + standardDeviation * z0;
    }
}
