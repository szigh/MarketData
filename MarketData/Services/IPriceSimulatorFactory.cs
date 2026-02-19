using MarketData.Models;
using MarketData.PriceSimulator;

namespace MarketData.Services;

/// <summary>
/// Factory interface for creating price simulator instances
/// </summary>
public interface IPriceSimulatorFactory
{
    /// <summary>
    /// Creates the appropriate price simulator for an instrument based on its model type and configuration
    /// </summary>
    IPriceSimulator CreateSimulator(Instrument instrument);
}
