using MarketData.Wpf.Client.Services;

namespace MarketData.Wpf.Client.ViewModels.ModelConfigs;

public class FlatConfigViewModel : ModelConfigViewModelBase
{
    public FlatConfigViewModel(string instrumentName, IDialogService dialogService)
        : base(instrumentName, dialogService)
    {
    }

    protected override Task<bool> TryExecutePublishConfigChangesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }
}
