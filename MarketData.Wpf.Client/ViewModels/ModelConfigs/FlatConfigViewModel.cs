using MarketData.Wpf.Client.Services;
using Microsoft.Extensions.Logging;

namespace MarketData.Wpf.Client.ViewModels.ModelConfigs;

public class FlatConfigViewModel : ModelConfigViewModelBase
{
    public FlatConfigViewModel(string instrumentName, IDialogService dialogService, ILogger<FlatConfigViewModel> logger)
        : base(instrumentName, dialogService, logger)
    {
    }

    protected override Task<bool> TryExecutePublishConfigChangesAsync(CancellationToken ct = default) 
        => Task.FromResult(true);

    public override bool ValidateProperties() => true;
}
