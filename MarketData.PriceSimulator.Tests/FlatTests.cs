namespace MarketData.PriceSimulator.Tests;

public class FlatTests
{
    [Fact]
    public void Constructor_CreatesInstance()
    {
        var flat = new Flat();

        Assert.NotNull(flat);
    }

    [Theory]
    [InlineData(100.0)]
    [InlineData(0.0)]
    [InlineData(-50.5)]
    [InlineData(1000000.0)]
    public async Task GenerateNextPrice_ReturnsUnchangedPrice(double price)
    {
        var flat = new Flat();
        var nextPrice = await flat.GenerateNextPrice(price);
        Assert.Equal(price, nextPrice);
    }

    [Fact]
    public async Task GenerateNextPrice_MultipleCallsReturnSameValue()
    {
        var flat = new Flat();
        var initialPrice = 123.45;

        var price1 = await flat.GenerateNextPrice(initialPrice);
        var price2 = await flat.GenerateNextPrice(price1);
        var price3 = await flat.GenerateNextPrice(price2);

        Assert.Equal(initialPrice, price1);
        Assert.Equal(initialPrice, price2);
        Assert.Equal(initialPrice, price3);
    }

    [Fact]
    public async Task GenerateNextPrice_ImplementsIPriceSimulator()
    {
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
        IPriceSimulator simulator = new Flat();
#pragma warning restore CA1859 // Use concrete types when possible for improved performance

        var nextPrice = await simulator.GenerateNextPrice(100.0);

        Assert.Equal(100.0, nextPrice);
    }
}
