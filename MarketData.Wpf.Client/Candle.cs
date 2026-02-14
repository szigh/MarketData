using FancyCandles;
using System.Collections.ObjectModel;

namespace MarketData.Wpf.Client
{
    internal class Candle : ICandle
    {
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
