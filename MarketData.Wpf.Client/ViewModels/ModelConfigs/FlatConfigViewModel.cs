using MarketData.Client.Wpf.Services;
using Microsoft.Extensions.Logging;

namespace MarketData.Client.Wpf.ViewModels.ModelConfigs;

public class FlatConfigViewModel : ModelConfigParamsViewModelBase
{
    public FlatConfigViewModel(string instrumentName, IDialogService dialogService, ILogger<FlatConfigViewModel> logger)
        : base(instrumentName, dialogService, logger)
    {
    }

    protected override Task<bool> TryExecutePublishConfigChangesAsync(CancellationToken ct = default) 
        => Task.FromResult(true);

    public override bool ValidateProperties() => true;
}
