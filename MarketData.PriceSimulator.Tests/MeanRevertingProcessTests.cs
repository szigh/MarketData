namespace MarketData.PriceSimulator.Tests;

public class MeanRevertingProcessTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var process = new MeanRevertingProcess(
            mean: 100.0,
            kappa: 0.5,
            sigma: 2.0,
            dt: 0.01);

        Assert.NotNull(process);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    public void Constructor_WithInvalidKappa_ThrowsArgumentException(double kappa)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new MeanRevertingProcess(100.0, kappa, 2.0, 0.01));

        Assert.Equal("kappa", exception.ParamName);
        Assert.Contains("Mean reversion strength must be a positive value", exception.Message);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(-0.1)]
    public void Constructor_WithNegativeSigma_ThrowsArgumentException(double sigma)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new MeanRevertingProcess(100.0, 0.5, sigma, 0.01));

        Assert.Equal("sigma", exception.ParamName);
        Assert.Contains("Volatility cannot be negative", exception.Message);
    }

    [Fact]
    public void Constructor_WithZeroSigma_IsValid()
    {
        var process = new MeanRevertingProcess(100.0, 0.5, sigma: 0.0, dt: 0.01);

        Assert.NotNull(process);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    public void Constructor_WithInvalidDt_ThrowsArgumentException(double dt)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new MeanRevertingProcess(100.0, 0.5, 2.0, dt));

        Assert.Equal("dt", exception.ParamName);
        Assert.Contains("Time step must be a positive value", exception.Message);
    }

    [Fact]
    public async Task GenerateNextPrice_ReturnsFiniteNumber()
    {
        var process = new MeanRevertingProcess(
            mean: 100.0,
            kappa: 0.5,
            sigma: 2.0,
            dt: 0.01);
        var currentPrice = 105.0;

        var nextPrice = await process.GenerateNextPrice(currentPrice);

        Assert.True(double.IsFinite(nextPrice), "Next price should be a finite number");
    }

    [Theory]
    [InlineData(100.0, 100.0, 0.5, 0.0, 0.01, 100.0)]  // At mean, zero volatility
    [InlineData(110.0, 100.0, 0.5, 0.0, 0.01, 109.95)] // Above mean, reverts down
    [InlineData(90.0, 100.0, 0.5, 0.0, 0.01, 90.05)]   // Below mean, reverts up
    [InlineData(120.0, 100.0, 1.0, 0.0, 0.01, 119.8)]  // Stronger kappa, faster reversion
    public async Task GenerateNextPrice_WithZeroVolatility_ShowsDeterministicMeanReversion(
        double startPrice, double mean, double kappa, double sigma, double dt, double expectedPrice)
    {
        var process = new MeanRevertingProcess(mean, kappa, sigma, dt);

        var nextPrice = await process.GenerateNextPrice(startPrice);

        Assert.Equal(expectedPrice, nextPrice, precision: 10);
    }

    [Fact]
    public async Task GenerateNextPrice_WithZeroVolatilityAtMean_PriceUnchanged()
    {
        var mean = 100.0;
        var process = new MeanRevertingProcess(
            mean: mean,
            kappa: 0.5,
            sigma: 0.0,
            dt: 0.01);

        var nextPrice = await process.GenerateNextPrice(mean);

        Assert.Equal(mean, nextPrice);
    }

    [Fact]
    public async Task GenerateNextPrice_WithNonZeroVolatility_ChangesPrice()
    {
        var process = new MeanRevertingProcess(
            mean: 100.0,
            kappa: 0.5,
            sigma: 2.0,
            dt: 0.01);
        var currentPrice = 100.0;

        var prices = new HashSet<double>();
        for (int i = 0; i < 20; i++)
        {
            var nextPrice = await process.GenerateNextPrice(currentPrice);
            prices.Add(nextPrice);
            currentPrice = nextPrice;
        }

        Assert.True(prices.Count > 1, "With non-zero volatility, prices should vary");
    }

    [Fact]
    public async Task GenerateNextPrice_ImplementsIPriceSimulator()
    {
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
        IPriceSimulator simulator = new MeanRevertingProcess(100.0, 0.5, 2.0, 0.01);
#pragma warning restore CA1859 // Use concrete types when possible for improved performance

        var nextPrice = await simulator.GenerateNextPrice(100.0);

        Assert.True(double.IsFinite(nextPrice));
    }

    [Theory]
    [InlineData(0.1, 100.0)]
    [InlineData(0.5, 100.0)]
    [InlineData(1.0, 100.0)]
    [InlineData(2.0, 100.0)]
    public async Task GenerateNextPrice_WithDifferentKappaValues_ProducesFiniteResults(double kappa, double mean)
    {
        var process = new MeanRevertingProcess(mean, kappa, sigma: 1.0, dt: 0.01);

        var nextPrice = await process.GenerateNextPrice(mean + 10.0);

        Assert.True(double.IsFinite(nextPrice));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(5.0)]
    public async Task GenerateNextPrice_WithDifferentSigmaValues_ProducesFiniteResults(double sigma)
    {
        var process = new MeanRevertingProcess(mean: 100.0, kappa: 0.5, sigma, dt: 0.01);

        var nextPrice = await process.GenerateNextPrice(100.0);

        Assert.True(double.IsFinite(nextPrice));
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.01)]
    [InlineData(0.1)]
    public async Task GenerateNextPrice_WithDifferentDtValues_ProducesFiniteResults(double dt)
    {
        var process = new MeanRevertingProcess(mean: 100.0, kappa: 0.5, sigma: 1.0, dt);

        var nextPrice = await process.GenerateNextPrice(100.0);

        Assert.True(double.IsFinite(nextPrice));
    }
}
