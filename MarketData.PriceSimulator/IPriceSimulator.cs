namespace MarketData.PriceSimulator;

public interface IPriceSimulator
{
    Task<double> GenerateNextPrice(double price);
}
