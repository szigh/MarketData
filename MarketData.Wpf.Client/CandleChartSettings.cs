using FancyCandles;
using System.ComponentModel.DataAnnotations;

namespace MarketData.Wpf.Client;

public class CandleChartSettings
{
    public const string SectionName = "CandleChart";

    [Required(ErrorMessage = "CandlePrecision is required in CandleChart app settings")]
    [Range(1, 8, ErrorMessage = "CandlePrecision must be between 1 and 8")]
    public int CandlePrecision { get; set; }

    [Required(ErrorMessage = "CandleTimeFrame is required in CandleChart app settings")]
    [EnumDataType(typeof(TimeFrame), ErrorMessage = "CandleTimeFrame must be a valid TimeFrame value. " +
        "Example accepted values are 1 (1 minute), 5 (5 minutes), 60 (1 hour), -1 (1 second), -5 (5 seconds), -30 (30 seconds). " +
        "See FancyCandles.TimeFrame enum for a complete list of accepted values.")]
    public TimeFrame CandleTimeFrame { get; set; }

    [Required(ErrorMessage = "LoadHistoryOnStartMinutes is required in CandleChart app settings")]
    [Range(1, 26280000, ErrorMessage = "LoadHistoryOnStartMinutes must be between 1 minute and 26280000 minutes (50 years)")]
    public int LoadHistoryOnStartMinutes { get; set; }
}
