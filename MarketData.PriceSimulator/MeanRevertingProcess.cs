using Microsoft.Extensions.Logging;

namespace MarketData.PriceSimulator;

/// <summary>
/// Ornstein-Uhlenbeck process
/// </summary>
public class MeanRevertingProcess : IPriceSimulator
{
    private readonly ILogger<MeanRevertingProcess>? _logger;

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
    public MeanRevertingProcess(double mean, double kappa, double sigma, double dt)
        : this(mean, kappa, sigma, dt, null)
    {
    }

    /// <summary>
    /// Ornstein-Uhlenbeck mean reverting process
    /// </summary>
    /// <param name="mean">μ</param>
    /// <param name="kappa">Mean reversion strength</param>
    /// <param name="sigma">Volatility</param>
    /// <param name="dt">Time step</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    public MeanRevertingProcess(
        double mean,
        double kappa,
        double sigma,
        double dt, 
        ILogger<MeanRevertingProcess>? logger)
    {
        _logger = logger;

        if (kappa <= 0)
        {
            _logger?.LogError("Invalid mean reversion strength: {Kappa}. Must be positive.", kappa);
            throw new ArgumentException("Mean reversion strength must be a positive value.", nameof(kappa));
        }
        if (sigma < 0)
        {
            _logger?.LogError("Invalid volatility: {Sigma}. Must be non-negative.", sigma);
            throw new ArgumentException("Volatility cannot be negative.", nameof(sigma));
        }
        if (dt <= 0)
        {
            _logger?.LogError("Invalid time step: {Dt}. Must be positive.", dt);
            throw new ArgumentException("Time step must be a positive value.", nameof(dt));
        }

        _mean = mean;
        _kappa = kappa;
        _sigma = sigma;
        _dt = dt;

        _logger?.LogDebug("Created MeanRevertingProcess with Mean={Mean}, Kappa={Kappa}, Sigma={Sigma}, Dt={Dt}",
            mean, kappa, sigma, dt);
    }

    public async Task<double> GenerateNextPrice(double price)
    { 
        var z = NormalDistribution.Generate(0, 1);

        var drift = _kappa * (_mean - price) * _dt;
        var diffusion = _sigma * Math.Sqrt(_dt) * z;

        _logger?.LogTrace("Calculated drift={Drift} and diffusion={Diffusion} for price {Price}", drift, diffusion, price);
        
        return price + drift + diffusion;
    }
}
