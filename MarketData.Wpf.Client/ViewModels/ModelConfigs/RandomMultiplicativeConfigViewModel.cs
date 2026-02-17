using MarketData.Grpc;

namespace MarketData.Wpf.Client.ViewModels.ModelConfigs;

public class RandomMultiplicativeConfigViewModel : ModelConfigViewModelBase
{
    private readonly RandomMultiplicativeConfigData _config;

    public RandomMultiplicativeConfigViewModel(string instrumentName, RandomMultiplicativeConfigData config)
        : base(instrumentName)
    {
        _config = config;
    }

    public double StandardDeviation => _config.StandardDeviation;
    public double Mean => _config.Mean;

    protected override Task<bool> TryExecutePublishConfigChangesAsync()
    {
        throw new NotImplementedException();
    }
}
