using MarketData.Wpf.Client.Services;
using MarketData.Wpf.Shared;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace MarketData.Wpf.Client.ViewModels.ModelConfigs;

/// <summary>
/// Base class for model-specific configuration ViewModels
/// </summary>
public abstract class ModelConfigViewModelBase : ViewModelBase
{
    protected string _instrumentName;
    protected bool _isModified = false;
    protected readonly IDialogService _dialogService;
    protected readonly ILogger<ModelConfigViewModelBase> _logger;

    protected ModelConfigViewModelBase(string instrumentName, IDialogService dialogService, ILogger<ModelConfigViewModelBase> logger)
    {
        _instrumentName = instrumentName;
        _dialogService = dialogService;
        _logger = logger;
    }

    internal async Task ExecutePublishConfigChangesSafe(CancellationToken ct = default)
    {
        bool success;
        try
        {
            _logger.LogInformation("Attempting to publish config changes for instrument {Instrument}.", _instrumentName);
            success = await TryExecutePublishConfigChangesAsync(ct);
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning(vex, "Validation error while publishing config changes for instrument {Instrument}: {Message}",
                _instrumentName, vex.Message);
            _dialogService.ShowWarning($"Validation error: {vex.Message}",
                "Validation error");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while publishing config changes for instrument {Instrument}: {Message}",
                _instrumentName, ex.Message);
            _dialogService.ShowError($"Failed to publish config changes: {ex.Message}",
                "Error publishing config changes");
            throw;
        }

        if (success)
            IsModified = false;
        else
        {
            _logger.LogWarning("Publishing config changes for instrument {Instrument} did not succeed, but no exception was thrown. " +
                "This likely means the TryExecutePublishConfigChangesAsync method returned false due to validation failure. " +
                "Please check your input and try again.", _instrumentName);
            _dialogService.ShowWarning(
                 $"Failed to publish config changes. " +
                 $"Please check your input and try again.");
        }
    }

    public string InstrumentName => _instrumentName;

    public bool IsModified
    {
        get => _isModified;
        protected set => SetProperty(ref _isModified, value);
    }

    protected abstract Task<bool> TryExecutePublishConfigChangesAsync(CancellationToken ct = default);
    protected abstract bool ValidateProperties();
}
