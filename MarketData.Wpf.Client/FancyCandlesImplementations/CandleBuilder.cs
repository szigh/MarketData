using Microsoft.Extensions.Logging;
using System.Numerics;

namespace MarketData.Wpf.Client.FancyCandlesImplementations;

/// <param name="timeSpan">The "width" (in time) of the candles</param>
/// <param name="logger">The logger instance for logging</param>
/// <param name="connectCandles">Whether to connect candles (Close of previous candle = Open of next candle)</param>
internal class CandleBuilder<T>(TimeSpan timeSpan, ILogger logger, bool connectCandles = false) where T : INumber<T>
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private DateTime _start;
    private bool _isOpen = false;
    private T _lastClose;
    private T _open;
    private T _high;
    private T _low;
    private T _close;
    private int _volume;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public (T o, T h, T l, T c, int count)? AddPoint(DateTime t, T v)
    {
        logger.LogDebug("Adding point to candle: Time={Time:yyyy-MM-dd HH:mm:ss.fff zzz}, Value={Value}", t, v);
        if (!_isOpen)
        {
            _isOpen = true;
            _start = t;

            if (connectCandles && _lastClose != default)
                _open = _lastClose;
            else
                _open = v;

            _high = v;
            _low = v;
            _close = v;
            _volume = 1;
            return null;
        }
        else
        {
            _volume++;
            if (v > _high)
            {
                _high = v;
            }
            if (v < _low)
            {
                _low = v;
            }
            if ((t - _start) > timeSpan)
            {
                //need to close the candle
                _close = v;
                _lastClose = v;
                _isOpen = false;

                logger.LogDebug("Candle completed: Start={Start:yyyy-MM-dd HH:mm:ss.fff zzz}, " +
                    "Open={Open}, High={High}, Low={Low}, Close={Close}, Volume={Volume}",
                    _start, _open, _high, _low, _close, _volume);

                return (_open, _high, _low, _close, _volume);
            }
            else
            {
                return null;
            }
        }
    }
}
