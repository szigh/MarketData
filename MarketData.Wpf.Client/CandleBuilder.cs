using System.Numerics;

namespace MarketData.Wpf.Client
{
    internal class CandleBuilder<T> where T : INumber<T>
    {
        private readonly TimeSpan _timeSpan;
        private readonly bool _connectCandles;

        private DateTime _start;
        private bool _isOpen = false;
        private T _lastClose;
        private T _open;
        private T _high;
        private T _low;
        private T _close;
        private int _count;

        public CandleBuilder(TimeSpan timeSpan, bool connectCandles = false)
        {
            _timeSpan = timeSpan;
            _connectCandles = connectCandles;
        }

        public (T o, T h, T l, T c, int count)? AddPoint(DateTime t, T v)
        {
            if (!_isOpen)
            {
                _isOpen = true;
                _start = t;

                if (_connectCandles && _lastClose != default)
                    _open = _lastClose;
                else
                    _open = v;

                _high = v;
                _low = v;
                _close = v;
                _count = 1;
                return null;
            }
            else
            {
                _count++;
                if (v > _high)
                {
                    _high = v;
                }
                if (v < _low)
                {
                    _low = v;
                }
                if ((t - _start) > _timeSpan)
                {
                    //need to close the candle
                    _close = v;
                    _lastClose = v;
                    _isOpen = false;
                    return (_open, _high, _low, _close, _count);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
