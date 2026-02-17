using MarketData.Wpf.Shared;

namespace MarketData.Wpf.Client.ViewModels.ModelConfigs;

/// <summary>
/// Base class for model-specific configuration ViewModels
/// </summary>
public abstract class ModelConfigViewModelBase : ViewModelBase
{
    protected string _instrumentName;

    protected ModelConfigViewModelBase(string instrumentName)
    {
        _instrumentName = instrumentName;
    }

    public string InstrumentName => _instrumentName;
}
