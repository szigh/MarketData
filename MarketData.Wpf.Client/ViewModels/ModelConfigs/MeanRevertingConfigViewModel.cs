using MarketData.Grpc;

namespace MarketData.Wpf.Client.ViewModels.ModelConfigs;

public class MeanRevertingConfigViewModel : ModelConfigViewModelBase
{
    private readonly MeanRevertingConfigData _config;

    public MeanRevertingConfigViewModel(string instrumentName, MeanRevertingConfigData config)
        : base(instrumentName)
    {
        _config = config;
    }

    public double Mean => _config.Mean;
    public double Kappa => _config.Kappa;
    public double Sigma => _config.Sigma;
    public double Dt => _config.Dt;

    protected override Task<bool> TryExecutePublishConfigChangesAsync()
    {
        throw new NotImplementedException();
    }
}
