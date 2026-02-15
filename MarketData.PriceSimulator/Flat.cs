namespace MarketData.PriceSimulator;

public class Flat : IPriceSimulator
{
    public async Task<double> GenerateNextPrice(double price)
    {
        return price;
    }
}
