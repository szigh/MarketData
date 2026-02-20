namespace MarketData.PriceSimulator.Tests;

public class RandomMultiplicativeProcessTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var process = new RandomMultiplicativeProcess(
            standardDeviation: 0.02,
            mean: 0.0);

        Assert.NotNull(process);
    }

    [Fact]
    public void Constructor_WithDefaultMean_CreatesInstance()
    {
        var process = new RandomMultiplicativeProcess(standardDeviation: 0.02);

        Assert.NotNull(process);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-1.0)]
    public void Constructor_WithInvalidStandardDeviation_ThrowsArgumentException(double standardDeviation)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new RandomMultiplicativeProcess(standardDeviation));

        Assert.Equal("standardDeviation", exception.ParamName);
        Assert.Contains("Standard deviation must be a positive value", exception.Message);
    }

    [Theory]
    [InlineData(0.01, 0.0)]
    [InlineData(0.02, 0.0)]
    [InlineData(0.05, 0.001)]
    [InlineData(0.1, -0.001)]
    [InlineData(5, 1)]
    [InlineData(1, -1)]
    public void Constructor_WithVariousValidParameters_CreatesInstance(double standardDeviation, double mean)
    {
        var process = new RandomMultiplicativeProcess(standardDeviation, mean);

        Assert.NotNull(process);
    }

    [Fact]
    public async Task GenerateNextPrice_ReturnsFiniteNumber()
    {
        var process = new RandomMultiplicativeProcess(
            standardDeviation: 0.02,
            mean: 0.0);
        var currentPrice = 100.0;

        var nextPrice = await process.GenerateNextPrice(currentPrice);

        Assert.True(double.IsFinite(nextPrice), "Next price should be a finite number");
    }

    [Theory]
    [InlineData(100.0, 0.0, 100.0)]   // Zero percentage move
    [InlineData(100.0, 0.1, 110.0)]   // 10% increase
    [InlineData(100.0, -0.1, 90.0)]   // 10% decrease
    [InlineData(50.0, 0.5, 75.0)]     // 50% increase
    [InlineData(200.0, -0.25, 150.0)] // 25% decrease
    public void CalculateMultiplicativeChange_FollowsFormula(double startPrice, double percentageMove, double expectedPrice)
    {
        var calculatedPrice = startPrice * (1 + percentageMove);

        Assert.Equal(expectedPrice, calculatedPrice, precision: 10);
    }

    [Fact]
    public async Task GenerateNextPrice_ImplementsIPriceSimulator()
    {
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
        IPriceSimulator simulator = new RandomMultiplicativeProcess(0.02);
#pragma warning restore CA1859 // Use concrete types when possible for improved performance

        var nextPrice = await simulator.GenerateNextPrice(100.0);

        Assert.True(double.IsFinite(nextPrice));
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.01)]
    [InlineData(0.1)]
    [InlineData(0.5)]
    public async Task GenerateNextPrice_WithDifferentVolatilities_ProducesFiniteResults(double standardDeviation)
    {
        var process = new RandomMultiplicativeProcess(standardDeviation);

        var nextPrice = await process.GenerateNextPrice(100.0);

        Assert.True(double.IsFinite(nextPrice));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(0.0)]
    [InlineData(0.01)]
    [InlineData(0.05)]
    public async Task GenerateNextPrice_WithDifferentMeans_ProducesFiniteResults(double mean)
    {
        var process = new RandomMultiplicativeProcess(standardDeviation: 0.02, mean: mean);

        var nextPrice = await process.GenerateNextPrice(100.0);

        Assert.True(double.IsFinite(nextPrice));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(10.0)]
    [InlineData(100.0)]
    [InlineData(1000.0)]
    public async Task GenerateNextPrice_WithDifferentStartPrices_ProducesFiniteResults(double startPrice)
    {
        var process = new RandomMultiplicativeProcess(standardDeviation: 0.02);

        var nextPrice = await process.GenerateNextPrice(startPrice);

        Assert.True(double.IsFinite(nextPrice));
    }
}
