using System;
using System.Collections.Generic;
using System.Text;

namespace MarketData.PriceSimulator
{
    public interface IPriceSimulator
    {
        Task<double> GenerateNextPrice(double price);
    }
}
