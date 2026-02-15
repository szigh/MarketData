///Quick console application to quickly see how my models generate prices
using MarketData.PriceSimulator;

// 99% of moves stay within 1% of current price
// 99% ≈ ±2.576 standard deviations
// Therefore: σ = 0.01 / 2.576 ≈ 0.00388
var standardDeviation = 0.00388;

var largeSD = 0.1;

var simulator = new RandomMultiplicativeProcess(largeSD);
var price = 10000d;
Console.Write(price);

while (true)
{
    price = await simulator.GenerateNextPrice(price);
    Console.Clear();
    Console.Write(price);
}