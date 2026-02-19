using MarketData.Grpc;
using MarketData.Wpf.Client.Services;

namespace MarketData.Wpf.Client.ViewModels.ModelConfigs;

public class RandomMultiplicativeConfigViewModel : ModelConfigViewModelBase
{
    private readonly ModelConfigService _modelConfigService;
    private double _standardDeviation;
    private double _mean;

    public RandomMultiplicativeConfigViewModel(string instrumentName, RandomMultiplicativeConfigData config,
        ModelConfigService modelConfigService)
        : base(instrumentName)
    {
        _mean = config.Mean;
        _standardDeviation = config.StandardDeviation;
        _modelConfigService = modelConfigService;
    }

    public double StandardDeviation
    {
        get => _standardDeviation;
        set
        {
            SetProperty(ref _standardDeviation, value);
            IsModified = true;
        }
    }
    public double Mean
    {
        get => _mean;
        set
        {
            SetProperty(ref _mean, value);
            IsModified = true;
        }
    }

    protected override async Task<bool> TryExecutePublishConfigChangesAsync()
    {
        await _modelConfigService.UpdateRandomMultiplicativeConfigAsync(InstrumentName, StandardDeviation, Mean);
        return true;
    }
}
