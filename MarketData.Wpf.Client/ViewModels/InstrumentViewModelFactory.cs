using MarketData.Client.Grpc.Services;
using MarketData.Client.Wpf.Services;
using MarketData.Wpf.Client.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketData.Wpf.Client.ViewModels;

public class InstrumentViewModelFactory
{
    private readonly IPriceService _priceService;
    private readonly IModelConfigService _modelConfigService;
    private readonly IDialogService _dialogService;
    private readonly IOptions<CandleChartSettings> _options;
    private readonly ILoggerFactory _loggerFactory;

    public InstrumentViewModelFactory(
        IPriceService priceService,
        IModelConfigService modelConfigService,
        IDialogService dialogService,
        IOptions<CandleChartSettings> options,
        ILoggerFactory loggerFactory)
    {
        _priceService = priceService;
        _modelConfigService = modelConfigService;
        _dialogService = dialogService;
        _options = options;
        _loggerFactory = loggerFactory;
    }

    public InstrumentViewModel Create(string instrumentName)
    {
        return new InstrumentViewModel(
            instrumentName,
            _priceService,
            _modelConfigService,
            _dialogService,
            _options,
            _loggerFactory);
    }
}
