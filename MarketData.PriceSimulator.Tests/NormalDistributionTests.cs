namespace MarketData.PriceSimulator.Tests;

public class NormalDistributionTests
{
    [Fact]
    public void Generate_WithValidParameters_ReturnsFiniteNumber()
    {
        var result = NormalDistribution.Generate(mean: 0.0, standardDeviation: 1.0);

        Assert.True(double.IsFinite(result));
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(-0.1)]
    public void Generate_WithNegativeStandardDeviation_ThrowsArgumentException(double standardDeviation)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            NormalDistribution.Generate(mean: 0.0, standardDeviation));

        Assert.Equal("standardDeviation", exception.ParamName);
        Assert.Contains("Standard deviation must be non-negative", exception.Message);
    }

    [Fact]
    public void Generate_WithZeroStandardDeviation_ReturnsExactlyMean()
    {
        var mean = 42.0;
        var standardDeviation = 0.0;

        var result = NormalDistribution.Generate(mean, standardDeviation);

        Assert.Equal(mean, result);
    }

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(100.0, 10.0)]
    [InlineData(-50.0, 5.0)]
    [InlineData(0.5, 0.1)]
    public void Generate_WithVariousParameters_ReturnsFiniteNumbers(double mean, double standardDeviation)
    {
        var result = NormalDistribution.Generate(mean, standardDeviation);

        Assert.True(double.IsFinite(result));
    }

    [Fact]
    public void Generate_WithNonZeroStandardDeviation_ProducesVariedResults()
    {
        var results = new HashSet<double>();

        for (int i = 0; i < 100; i++)
        {
            var result = NormalDistribution.Generate(mean: 0.0, standardDeviation: 1.0);
            results.Add(result);
        }

        Assert.True(results.Count > 10, 
            $"Expected varied results with non-zero standard deviation. Got {results.Count} unique values out of 100.");
    }

    [Fact]
    public void Generate_WithZeroStandardDeviation_AlwaysReturnsMean()
    {
        var mean = 123.456;
        var results = new HashSet<double>();

        for (int i = 0; i < 10; i++)
        {
            var result = NormalDistribution.Generate(mean, standardDeviation: 0.0);
            results.Add(result);
        }

        Assert.Single(results);
        Assert.Equal(mean, results.First());
    }

    [Fact]
    public void Generate_MultipleCallsWithSameParameters_ProducesDifferentResults()
    {
        var mean = 10.0;
        var standardDeviation = 2.0;

        var result1 = NormalDistribution.Generate(mean, standardDeviation);
        var result2 = NormalDistribution.Generate(mean, standardDeviation);
        var result3 = NormalDistribution.Generate(mean, standardDeviation);

        var allSame = result1 == result2 && result2 == result3;
        Assert.False(allSame, "Expected different values from multiple calls (very high probability with σ>0)");
    }
}
