using MarketData.Grpc;
using MarketData.Wpf.Client.Services;

namespace MarketData.Wpf.Client.ViewModels;

public class InstrumentViewModelFactory
{
    private readonly MarketDataService.MarketDataServiceClient _grpcClient;
    private readonly IModelConfigService _modelConfigService;

    public InstrumentViewModelFactory(
        MarketDataService.MarketDataServiceClient grpcClient,
        IModelConfigService modelConfigService)
    {
        _grpcClient = grpcClient;
        _modelConfigService = modelConfigService;
    }

    public InstrumentViewModel Create(string instrumentName)
    {
        return new InstrumentViewModel(_grpcClient, _modelConfigService, instrumentName);
    }
}
