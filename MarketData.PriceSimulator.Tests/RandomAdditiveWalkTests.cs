namespace MarketData.PriceSimulator.Tests;

public class RandomAdditiveWalkTests
{
    [Fact]
    public void Constructor_WithValidSteps_CreatesInstance()
    {
        var steps = new RandomWalkSteps(
        [
            new(0.5, 1.0),
            new(0.5, -1.0)
        ]);

        var walk = new RandomAdditiveWalk(steps);

        Assert.NotNull(walk);
    }

    [Fact]
    public void RandomWalkSteps_WithProbabilitiesSummingToOne_IsValid()
    {
        var steps = new RandomWalkSteps(
        [
            new(0.25, 2.0),
            new(0.25, 1.0),
            new(0.25, -1.0),
            new(0.25, -2.0)
        ]);

        Assert.NotNull(steps);
        Assert.Equal(4, steps.WalkSteps.Count);
    }

    [Fact]
    public void RandomWalkSteps_WithProbabilitiesNotSummingToOne_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new RandomWalkSteps(
            [
                new(0.6, 1.0),
                new(0.5, -1.0)
            ]));

        Assert.Contains("Probabilities must sum to 1", exception.Message);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void RandomWalkSteps_WithInvalidProbability_ThrowsArgumentException(double invalidProbability)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new RandomWalkSteps(
            [
                new(invalidProbability, 1.0),
                new(1.0 - invalidProbability, -1.0)
            ]));

        Assert.Contains("Probabilities cannot be negative or greater than 1", exception.Message);
    }

    [Fact]
    public void RandomWalkSteps_WithProbabilitiesCloseToOne_IsValid()
    {
        var steps = new RandomWalkSteps(
        [
            new(0.333333, 1.0),
            new(0.333333, 0.0),
            new(0.333334, -1.0)
        ]);

        Assert.NotNull(steps);
    }

    [Fact]
    public async Task GenerateNextPrice_ReturnsFiniteNumber()
    {
        var steps = new RandomWalkSteps(
        [
            new(0.5, 1.0),
            new(0.5, -1.0)
        ]);
        var walk = new RandomAdditiveWalk(steps);
        var currentPrice = 100.0;

        var nextPrice = await walk.GenerateNextPrice(currentPrice);

        Assert.True(double.IsFinite(nextPrice), "Next price should be a finite number");
    }

    [Theory]
    [InlineData(100.0, 0.0, 100.0)]
    [InlineData(100.0, 5.0, 105.0)]
    [InlineData(50.0, -10.0, 40.0)]
    public async Task GenerateNextPrice_WithSingleStep_ReturnsExpectedChange(double startPrice, double stepValue, double expectedPrice)
    {
        var steps = new RandomWalkSteps([new(1.0, stepValue)]);
        var walk = new RandomAdditiveWalk(steps);

        var nextPrice = await walk.GenerateNextPrice(startPrice);

        Assert.Equal(expectedPrice, nextPrice);
    }

    [Fact]
    public async Task GenerateNextPrice_AppliesAdditiveChange()
    {
        var steps = new RandomWalkSteps(
        [
            new(0.5, 3.5),
            new(0.5, -1.5)
        ]);
        var walk = new RandomAdditiveWalk(steps);
        var initialPrice = 50.0;

        var nextPrice = await walk.GenerateNextPrice(initialPrice);

        Assert.Contains(nextPrice, new[] { 53.5, 48.5 });
    }

    [Fact]
    public async Task GenerateNextPrice_ImplementsIPriceSimulator()
    {
        var steps = new RandomWalkSteps(
        [
            new(0.5, 1.0),
            new(0.5, -1.0)
        ]);
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
        IPriceSimulator simulator = new RandomAdditiveWalk(steps);
#pragma warning restore CA1859 // Use concrete types when possible for improved performance

        var nextPrice = await simulator.GenerateNextPrice(100.0);

        Assert.True(double.IsFinite(nextPrice));
    }

    [Fact]
    public async Task GenerateNextPrice_WithMultipleSteps_ReturnsOneOfTheStepValues()
    {
        var steps = new RandomWalkSteps(
        [
            new(0.25, 5.0),
            new(0.25, 2.0),
            new(0.25, -2.0),
            new(0.25, -5.0)
        ]);
        var walk = new RandomAdditiveWalk(steps);
        var currentPrice = 100.0;

        var nextPrice = await walk.GenerateNextPrice(currentPrice);
        var change = nextPrice - currentPrice;

        Assert.Contains(change, new[] { 5.0, 2.0, -2.0, -5.0 });
    }

    [Fact]
    public void RandomWalkStep_StoresValues()
    {
        var step = new RandomWalkStep(0.3, 1.5);

        Assert.Equal(0.3, step.Probability);
        Assert.Equal(1.5, step.Value);
    }

    [Fact]
    public void RandomWalkSteps_PreservesStepOrder()
    {
        var inputSteps = new List<RandomWalkStep>
        {
            new(0.1, 10.0),
            new(0.2, 20.0),
            new(0.3, 30.0),
            new(0.4, 40.0)
        };

        var walkSteps = new RandomWalkSteps(inputSteps);

        Assert.Equal(4, walkSteps.WalkSteps.Count);
        Assert.Equal(10.0, walkSteps.WalkSteps[0].Value);
        Assert.Equal(20.0, walkSteps.WalkSteps[1].Value);
        Assert.Equal(30.0, walkSteps.WalkSteps[2].Value);
        Assert.Equal(40.0, walkSteps.WalkSteps[3].Value);
    }
}
