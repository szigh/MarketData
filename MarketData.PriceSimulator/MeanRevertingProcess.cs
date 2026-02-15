namespace MarketData.PriceSimulator;

/// <summary>
/// Ornstein-Uhlenbeck process
/// </summary>
public class MeanRevertingProcess : IPriceSimulator
{
    private readonly double _mean;
    private readonly double _kappa;
    private readonly double _sigma;
    private readonly double _dt;

    /// <summary>
    /// Ornstein-Uhlenbeck mean reverting process
    /// </summary>
    /// <param name="mean">μ</param>
    /// <param name="kappa">Mean reversion strength</param>
    /// <param name="sigma">Volatility</param>
    /// <param name="dt">Time step</param>
    public MeanRevertingProcess(
        double mean,
        double kappa,
        double sigma,
        double dt)
    {
        _mean = mean;
        _kappa = kappa;
        _sigma = sigma;
        _dt = dt;
    }

    public async Task<double> GenerateNextPrice(double price)
    {            
        var z = NormalDistribution.Generate(0, 1);

        var drift = _kappa * (_mean - price) * _dt;
        var diffusion = _sigma * Math.Sqrt(_dt) * z;

        return price + drift + diffusion;
    }
}
