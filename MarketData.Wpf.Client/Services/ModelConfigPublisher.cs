using MarketData.Grpc;
using MarketData.Wpf.Client.ViewModels.ModelConfigs;
using Microsoft.Extensions.Logging;

namespace MarketData.Wpf.Client.Services;

enum PublishResult { NotStarted, NotApplicable, Success, SuccessUnverified, Failure }

public class ModelConfigPublisher : IDisposable
{
    private readonly IDisposable? _logScope;
    private readonly ILogger _logger;
    private readonly IModelConfigService _modelConfigService;
    private readonly IDialogService _dialogService;

    private readonly string _instrument;
    private readonly string _activeModel;

    private PublishResult _modelParamsResult = 0;
    private PublishResult _activeModelResult = 0;
    private PublishResult _tickIntervalResult = 0;

    public ModelConfigPublisher(string instrument, string activeModel, 
        IModelConfigService modelConfigService, 
        IDialogService dialogService, 
        ILogger logger)
    {
        _logScope = logger.BeginScope($"[{nameof(ModelConfigPublisher)} Instrument={instrument}, ActiveModel={activeModel}]");
        _logger = logger;
        _modelConfigService = modelConfigService;
        _dialogService = dialogService;
        _instrument = instrument;
        _activeModel = activeModel;
    }

    public async Task<bool> PublishTickInterval(int tickIntervalMs, CancellationToken ct = default)
    {
        try
        {
            var result = await _modelConfigService.UpdateTickIntervalAsync(_instrument, tickIntervalMs, ct);
            if (result.Success)
            {
                //_tickIntervalChanged = false;
                _logger.LogInformation("{Status} Updated tick interval to {TickIntervalMs} ms for instrument {Instrument}.",
                    PublishResult.Success, tickIntervalMs, _instrument);
                _tickIntervalResult = PublishResult.Success;

                return true;
            }
            else
            {
                _dialogService.ShowError($"Failed to update tick interval. Server message: \"{result.Message}\"",
                    "Error updating tick interval");
                _logger.LogError("{Status} Failed to update tick interval to {TickIntervalMs} ms for instrument {Instrument}. " +
                    "Server message: \"{Message}\".",
                    PublishResult.Failure, tickIntervalMs, _instrument, result.Message);
                _tickIntervalResult = PublishResult.Failure;

                return false;
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Failed to update tick interval: {ex.Message}",
                "Error updating tick interval");
            _logger.LogError(ex, "{Status} Failed to update tick interval to {TickIntervalMs} ms for instrument {Instrument}.",
                PublishResult.Failure, tickIntervalMs, _instrument);
            _tickIntervalResult = PublishResult.Failure;

            return false;
        }
    }

    public async Task<(bool success, ConfigurationsResponse? configs)> TrySwitchModel(CancellationToken ct = default)
    {
        try
        {
            var result = await _modelConfigService.SwitchModelAsync(_instrument, _activeModel, ct);
            if (result.NewModel == _activeModel)
            {
                var configs = await _modelConfigService.GetConfigurationsAsync(_instrument, ct);

                _activeModelResult = PublishResult.Success;
                return (true, configs);
            }
            else
            {
                _dialogService.ShowError($"Model switch failed, active model is still {result.NewModel}. " +
                    $"Server message: \"{result.Message}\".", "Error switching model");
                _logger.LogError("{Status} Model switch failed, active model is still {NewModel} " +
                    "after attempting to switch to {ActiveModel} on instrument {Instrument}. " +
                    "Server message: {Message}.",
                    PublishResult.Failure, result.NewModel, _activeModel, _instrument, result.Message);

                _activeModelResult = PublishResult.Failure;

                return (false, null);
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Failed to switch model: {ex.Message}",
                "Error switching model");
            _logger.LogError(ex, "{Status} Failed to switch model to {ActiveModel} on instrument {Instrument}.",
                PublishResult.Failure, _activeModel, _instrument);
            _activeModelResult = PublishResult.Failure;

            return (false, null);
        }
    }

    public async Task<bool> TryPublishModelParams(ModelConfigViewModelBase activeConfigVm, CancellationToken ct = default)
    {
        try
        {
            await activeConfigVm.ExecutePublishConfigChangesSafe(ct);
            _logger.LogInformation("{Status} Published configuration changes for model {ActiveModel} " +
                "on instrument {Instrument}", PublishResult.SuccessUnverified, _activeModel, _instrument);
            _modelParamsResult = PublishResult.SuccessUnverified;

            return true;
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Failed to publish model configuration changes: {ex.Message}",
                "Error publishing changes");
            _logger.LogError(ex, "{Status} Failed to publish configuration changes for model {ActiveModel} " +
                "on instrument {Instrument}", PublishResult.Failure, _activeModel, _instrument);
            
             _modelParamsResult = PublishResult.Failure;

            return false;
        }
    }

    public void LogPublishResultsSummary()
    {
        var hasFailures = new[] { _modelParamsResult, _activeModelResult, _tickIntervalResult }
            .Any(r => r == PublishResult.Failure);
        var logLevel = hasFailures ? LogLevel.Warning : LogLevel.Information;
        _logger.Log(logLevel,
            "Publish summary for instrument {Instrument}: ModelParams={ModelParamsStatus}, " +
            "ActiveModel={ActiveModelStatus}, TickInterval={TickIntervalStatus}",
            _instrument, _modelParamsResult, _activeModelResult, _tickIntervalResult);

        if (hasFailures)
        {
            //TODO: downside of this is that it doesn't show detailed error/exception methods
            //TODO: would be nice to see which operations succeeded vs failed in the dialog

            var errorBuilder = new System.Text.StringBuilder();

            if (_modelParamsResult == PublishResult.Failure)
                errorBuilder.AppendLine("- Failed to publish model parameters.");
            if (_activeModelResult == PublishResult.Failure)
                errorBuilder.AppendLine("- Failed to switch active model.");
            if (_tickIntervalResult == PublishResult.Failure)
                errorBuilder.AppendLine("- Failed to update tick interval.");

            _dialogService.ShowError(errorBuilder.ToString(), "Publish summary");
        }
    }

    public void Dispose()
    {
        _logScope?.Dispose();
        _modelConfigService.Dispose();
    }
}
