using MarketData.Grpc;
using MarketData.Wpf.Client.Services;

namespace MarketData.Wpf.Client.ViewModels.ModelConfigs;

public class MeanRevertingConfigViewModel : ModelConfigViewModelBase
{
    private readonly ModelConfigService _modelConfigService;

    private double _mean;
    private double _kappa;
    private double _sigma;
    private double _dt;

    public MeanRevertingConfigViewModel(string instrumentName, MeanRevertingConfigData config, 
        ModelConfigService modelConfigService)
        : base(instrumentName)
    {
        _mean = config.Mean;
        _kappa = config.Kappa;
        _sigma = config.Sigma;
        _dt = config.Dt;
        _modelConfigService = modelConfigService;
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
    public double Kappa
    {
        get => _kappa;
        set
        {
            SetProperty(ref _kappa, value);
            IsModified = true;
        }
    }
    public double Sigma
    {
        get => _sigma;
        set
        {
            SetProperty(ref _sigma, value);
            IsModified = true;
        }
    }
    public double Dt 
    {
        get => _dt;
        set
        {
            SetProperty(ref _dt, value);
            IsModified = true;
        }
    }

    protected override async Task<bool> TryExecutePublishConfigChangesAsync()
    {
        await _modelConfigService.UpdateMeanRevertingConfigAsync(InstrumentName, Mean, Kappa, Sigma, Dt);
        return true;
    }
}
