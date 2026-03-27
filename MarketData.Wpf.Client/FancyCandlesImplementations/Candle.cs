using FancyCandles;
using System.Collections.ObjectModel;

namespace MarketData.Client.Wpf.FancyCandlesImplementations;

internal class Candle(DateTime t, (double o, double h, double l, double c, double v) ohlcv, int precision) : ICandle
{
    public DateTime t { get; set; } = t;
    public double O { get; set; } = double.Round(ohlcv.o, precision);
    public double H { get; set; } = double.Round(ohlcv.h, precision);
    public double L { get; set; } = double.Round(ohlcv.l, precision);
    public double C { get; set; } = double.Round(ohlcv.c, precision);
    public double V { get; set; } = ohlcv.v;
}

public class CandlesSource(TimeFrame timeFrame) : ObservableCollection<ICandle>, ICandlesSource
{
    public TimeFrame TimeFrame { get; } = timeFrame;
    bool ICollection<ICandle>.IsReadOnly => false;
}
