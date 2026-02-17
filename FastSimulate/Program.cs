///Quick console application to quickly see how my models generate prices
using MarketData.PriceSimulator;

// 99% of moves stay within 1% of current price
// 99% ≈ ±2.576 standard deviations
// Therefore: σ = 0.01 / 2.576 ≈ 0.00388
//var standardDeviation = 0.00388;

//var largeSD = 0.1;

//var simulator = new RandomMultiplicativeProcess(largeSD);

const double SECONDS_PER_YEAR = 252 * 6.5 * 3600; // 5,875,200

var simulator = new MeanRevertingProcess(
    mean: 10_000.0,
    kappa: 2.0 / SECONDS_PER_YEAR,              // ≈ 3.4e-7
    sigma: 0.2 / Math.Sqrt(SECONDS_PER_YEAR),   // ≈ 8.25e-5
    dt: 0.1  // 0.1 second time step
);
var price = 5d; //starting price
Console.Write(price);

while (true)
{
    price = await simulator.GenerateNextPrice(price);
    Console.Clear();
    Console.Write(price);
    Task.Delay(100).Wait();
}