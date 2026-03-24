using MarketData.Client.Wpf.Services;
using MarketData.Grpc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace MarketData.Client.Wpf.ViewModels.ModelConfigs;

public class MeanRevertingConfigViewModel : ModelConfigParamsViewModelBase
{
    private readonly IModelConfigService _modelConfigService;

    private double _mean;
    private double _kappa;
    private double _sigma;
    private double _dt;

    public MeanRevertingConfigViewModel(string instrumentName, MeanRevertingConfigData config, 
        IModelConfigService modelConfigService, IDialogService dialogService, ILogger<MeanRevertingConfigViewModel> logger)
        : base(instrumentName, dialogService, logger)
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

    protected override async Task<bool> TryExecutePublishConfigChangesAsync(CancellationToken ct = default)
    {
        if(!ValidateProperties())
            throw new ValidationException("Properties must satisfy: Sigma > 0, Dt >= 0, Kappa >= 0. Please check your input and try again.");

        await _modelConfigService.UpdateMeanRevertingConfigAsync(InstrumentName, Mean, Kappa, Sigma, Dt, ct);
        return true;
    }

    public override bool ValidateProperties()
    {
        return Sigma > 0 && Dt >= 0 && Kappa >= 0;
    }
}
