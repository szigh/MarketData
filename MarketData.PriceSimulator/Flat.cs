namespace MarketData.PriceSimulator;

internal class Flat : IPriceSimulator
{
    public async Task<double> GenerateNextPrice(double price)
    {
        return price;
    }
}
