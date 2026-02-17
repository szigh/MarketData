using FancyCandles;
using System.Collections.ObjectModel;

namespace MarketData.Wpf.Client.FancyCandlesImplementations
{
    internal class Candle : ICandle
    {
        public Candle(DateTime t, (double o, double h, double l, double c, double v) ohlcv, int precision)
        {
            this.t = t;
            O = double.Round(ohlcv.o, precision);
            H = double.Round(ohlcv.h, precision);
            L = double.Round(ohlcv.l, precision);
            C = double.Round(ohlcv.c, precision);
            V = ohlcv.v;
        }
        public DateTime t {get;set;}
        public double O {get;set;}
        public double H {get;set;}
        public double L {get;set;}
        public double C {get;set;}
        public double V {get;set;}
    }

    public class CandlesSource(TimeFrame timeFrame) : 
        ObservableCollection<ICandle>, ICandlesSource
    {
        private readonly TimeFrame _timeFrame = timeFrame;
        public TimeFrame TimeFrame { get => _timeFrame; }
    }
}
