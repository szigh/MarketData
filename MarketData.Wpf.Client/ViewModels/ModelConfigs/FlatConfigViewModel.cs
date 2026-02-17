namespace MarketData.Wpf.Client.ViewModels.ModelConfigs;

public class FlatConfigViewModel : ModelConfigViewModelBase
{
    public FlatConfigViewModel(string instrumentName)
        : base(instrumentName)
    {
    }

    protected override Task<bool> TryExecutePublishConfigChangesAsync()
    {
        return Task.FromResult(true);
    }
}
