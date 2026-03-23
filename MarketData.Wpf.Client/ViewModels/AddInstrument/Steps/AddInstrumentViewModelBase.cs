using MarketData.Wpf.Shared;

namespace MarketData.Client.Wpf.ViewModels.AddInstrument.Steps;

public abstract class AddInstrumentViewModelBase : ViewModelBase
{

    public bool IsValid()
    {
        UpdateValidationMessage();
        return ValidationMessage == "";
    }

    protected abstract void UpdateValidationMessage();
    public abstract string ValidationMessage { get; protected set; }
}
