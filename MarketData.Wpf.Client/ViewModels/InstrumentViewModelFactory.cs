using MarketData.Grpc;
using MarketData.Wpf.Client.Services;
using Microsoft.Extensions.Options;

namespace MarketData.Wpf.Client.ViewModels;

public class InstrumentViewModelFactory
{
    private readonly MarketDataService.MarketDataServiceClient _grpcClient;
    private readonly IModelConfigService _modelConfigService;
    private readonly IDialogService _dialogService;
    private readonly IOptions<CandleChartSettings> _options;

    public InstrumentViewModelFactory(
        MarketDataService.MarketDataServiceClient grpcClient,
        IModelConfigService modelConfigService,
        IDialogService dialogService,
        IOptions<CandleChartSettings> options)
    {
        _grpcClient = grpcClient;
        _modelConfigService = modelConfigService;
        _dialogService = dialogService;
        _options = options;
    }

    public InstrumentViewModel Create(string instrumentName)
    {
        return new InstrumentViewModel(_grpcClient, _modelConfigService, _dialogService, _options, instrumentName);
    }
}
