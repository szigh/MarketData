using MarketData.Wpf.Shared;

namespace MarketData.Client.Wpf.ViewModels.AddInstrument.Steps;

public abstract class AddInstrumentViewModelBase : ViewModelBase
{
    protected AddInstrumentViewModelBase()
    {
        // Automatically re-validate whenever any property changes
        PropertyChanged += (_, e) =>
        {
            // Don't re-validate when ValidationMessage or IsValid changes (avoid infinite loop)
            if (e.PropertyName != nameof(ValidationMessage) && e.PropertyName != nameof(IsValid))
            {
                OnValidationChanged();
            }
        };
    }

    public bool IsValid => string.IsNullOrEmpty(ValidationMessage);
    public event EventHandler? ValidationChanged;

    protected void OnValidationChanged()
    {
        UpdateValidationMessage();
        OnPropertyChanged(nameof(ValidationMessage));
        OnPropertyChanged(nameof(IsValid));
        ValidationChanged?.Invoke(this, EventArgs.Empty);
    }

    protected abstract void UpdateValidationMessage();
    public abstract string ValidationMessage { get; protected set; }
}
