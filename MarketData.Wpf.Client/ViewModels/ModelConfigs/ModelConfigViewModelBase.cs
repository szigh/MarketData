using MarketData.Wpf.Client.Services;
using MarketData.Wpf.Shared;

namespace MarketData.Wpf.Client.ViewModels.ModelConfigs;

/// <summary>
/// Base class for model-specific configuration ViewModels
/// </summary>
public abstract class ModelConfigViewModelBase : ViewModelBase
{
    protected string _instrumentName;
    protected bool _isModified = false;
    protected readonly IDialogService _dialogService;

    protected ModelConfigViewModelBase(string instrumentName, IDialogService dialogService)
    {
        _instrumentName = instrumentName;
        _dialogService = dialogService;
    }

    internal async Task ExecutePublishConfigChangesSafe(CancellationToken ct = default)
    {
        try
        {
            var success = await TryExecutePublishConfigChangesAsync(ct);
            if (success)
                IsModified = false;
            else
            {
                _dialogService.ShowWarning(
                     $"Failed to publish config changes. " +
                     $"Please check your input and try again.");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Failed to publish config changes: {ex.Message}",
                "Error publishing config changes");
        }
    }

    public string InstrumentName => _instrumentName;

    public bool IsModified
    {
        get => _isModified;
        set => SetProperty(ref _isModified, value);
    }

    protected abstract Task<bool> TryExecutePublishConfigChangesAsync(CancellationToken ct = default);
}
