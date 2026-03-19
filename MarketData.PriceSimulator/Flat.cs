using Microsoft.Extensions.Logging;

namespace MarketData.PriceSimulator;

public class Flat : IPriceSimulator
{
    private readonly ILogger<Flat>? _logger;

    public Flat() : this(null)
    {
    }

    public Flat(ILogger<Flat>? logger)
    {
        _logger = logger;
        _logger?.LogDebug("Created Flat simulator (no price movement)");
    }

    public async Task<double> GenerateNextPrice(double price)
    {
        _logger?.LogTrace("Flat price generation: {Price} -> {Price}", price, price);
        return price;
    }
}
