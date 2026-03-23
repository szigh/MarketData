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

    internal async Task<bool> ExecutePublishConfigChangesUnsafe(CancellationToken ct = default)
    {
        bool success;
        try
        {
            _logger.LogInformation("Attempting to publish config changes for instrument {Instrument}.", _instrumentName);
            success = await TryExecutePublishConfigChangesAsync(ct);

            if (success)
            {
                IsModified = false;
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public string InstrumentName => _instrumentName;

    public bool IsModified
    {
        get => _isModified;
        protected set => SetProperty(ref _isModified, value);
    }

    protected abstract Task<bool> TryExecutePublishConfigChangesAsync(CancellationToken ct = default);
    public abstract bool ValidateProperties();
}
