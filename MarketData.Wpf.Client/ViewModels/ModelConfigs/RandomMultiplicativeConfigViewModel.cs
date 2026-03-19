using MarketData.Client.Wpf.Services;
using MarketData.Grpc;
using MarketData.Wpf.Client.Services;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace MarketData.Wpf.Client.ViewModels.ModelConfigs;

public class RandomMultiplicativeConfigViewModel : ModelConfigViewModelBase
{
    private readonly IModelConfigService _modelConfigService;
    private double _standardDeviation;
    private double _mean;

    public RandomMultiplicativeConfigViewModel(string instrumentName, RandomMultiplicativeConfigData config,
        IModelConfigService modelConfigService, IDialogService dialogService, ILogger<RandomMultiplicativeConfigViewModel> logger)
        : base(instrumentName, dialogService, logger)
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

    protected override async Task<bool> TryExecutePublishConfigChangesAsync(CancellationToken ct)
    {
        if (!ValidateProperties())
        {
            throw new ValidationException("Invalid configuration: StandardDeviation must be greater than 0.");
        }

        await _modelConfigService.UpdateRandomMultiplicativeConfigAsync(InstrumentName, StandardDeviation, Mean, ct);
        return true;
    }

    protected override bool ValidateProperties()
    {
        return StandardDeviation > 0;
    }
}
