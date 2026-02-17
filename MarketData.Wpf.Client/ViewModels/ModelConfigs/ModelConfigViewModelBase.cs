using MarketData.Wpf.Shared;
using System.Windows;

namespace MarketData.Wpf.Client.ViewModels.ModelConfigs;

/// <summary>
/// Base class for model-specific configuration ViewModels
/// </summary>
public abstract class ModelConfigViewModelBase : ViewModelBase
{
    protected string _instrumentName;
    protected bool _isModified = true;

    protected ModelConfigViewModelBase(string instrumentName)
    {
        _instrumentName = instrumentName;
    }

    internal async Task ExecutePublishConfigChangesSafe()
    {
        try
        {
            var success = await TryExecutePublishConfigChangesAsync();
            if (success)
                _isModified = false;
            else
            {
                MessageBox.Show($"Failed to publish config changes. " +
                    $"Please check your input and try again.",
                    "Failed to publish config changes",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error publishing config changes: {ex}",
                "Error publishing config changes",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public string InstrumentName => _instrumentName;

    public bool IsModified
    {
        get => _isModified;
        set => SetProperty(ref _isModified, value);
    }

    protected abstract Task<bool> TryExecutePublishConfigChangesAsync();
}
