namespace MarketData.PriceSimulator;

public class RandomAdditiveWalk : IPriceSimulator
{
    private readonly Random _random = Random.Shared;
    private readonly RandomWalkSteps _walkSteps;

    public RandomAdditiveWalk(RandomWalkSteps walkSteps)
    {
        _walkSteps = walkSteps;
    }

    public Task<double> GenerateNextPrice(double price)
    {
        var x = _random.NextDouble();

        double cumulativeProbability = 0;
        foreach (var step in _walkSteps.WalkSteps)
        {
            cumulativeProbability += step.Probability;
            if (x < cumulativeProbability)
            {
                return Task.FromResult(price + step.Value);
            }
        }

        return Task.FromResult(price + _walkSteps.WalkSteps[^1].Value);
    }
}

public record RandomWalkStep
{
    public RandomWalkStep(double probability, double value)
    {
        Probability = probability;
        Value = value;
    }

    public double Probability { get; }
    public double Value { get; }
}

public record RandomWalkSteps
{
    public List<RandomWalkStep> WalkSteps { get; }

    public RandomWalkSteps(List<RandomWalkStep> walkSteps)
    {
        //check probabilities sum to 1 and probabilities are between 0 and 1
        var totalProbability = walkSteps.Sum(step => step.Probability);
        if (Math.Abs(totalProbability - 1) > 0.0001)
        {
            throw new ArgumentException("Probabilities must sum to 1.");
        }
        if (walkSteps.Any(step => step.Probability < 0 || step.Probability > 1))
        {
            throw new ArgumentException("Probabilities cannot be negative or greater than 1.");
        }

        WalkSteps = walkSteps;
    }
}