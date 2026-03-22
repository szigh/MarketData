using FancyCandles;
using Grpc.Core;
using MarketData.Client.Wpf.Services;
using MarketData.Grpc;
using MarketData.Wpf.Client.FancyCandlesImplementations;
using MarketData.Wpf.Client.Services;
using MarketData.Wpf.Client.Telemetry;
using MarketData.Wpf.Client.Views;
using MarketData.Wpf.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;
using System.Windows;
using System.Windows.Input;

namespace MarketData.Wpf.Client.ViewModels;

public class InstrumentViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly MarketDataService.MarketDataServiceClient _grpcClient;
    private readonly IModelConfigService _modelConfigService;
    private readonly IDialogService _dialogService;
    private readonly MarketDataClientActivitySource _activitySource;

    private readonly CandleBuilder<double> _candleBuilder;
    private CandlesSource _candles;
    private int _loadHistoryOnStartMinutes = 1440;
    private int _candlePrecision = 2;

    private string _price;
    private string _instrument;
    private string _timestamp;
    private bool _isStreaming;

    public InstrumentViewModel(string instrumentName,
        MarketDataService.MarketDataServiceClient grpcClient, 
        IModelConfigService modelConfigService,
        IDialogService dialogService,
        MarketDataClientActivitySource activitySource,
        IOptions<CandleChartSettings> candleChartConfig,
        ILoggerFactory loggerFactory)
    {
        _grpcClient = grpcClient;
        _modelConfigService = modelConfigService;
        _dialogService = dialogService;
        _activitySource = activitySource;
        _logger = loggerFactory.CreateLogger<InstrumentViewModel>();
        _loggerFactory = loggerFactory;
        _instrument = instrumentName;
        _price = "#.##";
        _timestamp = string.Empty;

        ModelConfigCommand = new AsyncRelayCommand(OpenModelConfigAsync);

        (_candleBuilder, _candles) = InitializeCandleChart(candleChartConfig);
    }

    private (CandleBuilder<double>, CandlesSource) InitializeCandleChart(IOptions<CandleChartSettings> candleChartConfig)
    {
        CandleChartSettings config;
        try
        {
            config = candleChartConfig.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load candle chart configuration");
            _dialogService.ShowError($"Invalid candle chart configuration: {ex.Message}", "Configuration Error");
            throw;
        }

        _logger.LogInformation("Initializing candle chart with time frame {TimeFrame}, precision {Precision}, " +
            "and history load of {HistoryMinutes} minutes",
            config.CandleTimeFrame, config.CandlePrecision, config.LoadHistoryOnStartMinutes);

        TimeFrame chartTimeFrame = config.CandleTimeFrame;
        _candlePrecision = config.CandlePrecision;
        _loadHistoryOnStartMinutes = config.LoadHistoryOnStartMinutes;

        return (new CandleBuilder<double>(chartTimeFrame.ToTimeSpan(), _logger, true), 
            new CandlesSource(chartTimeFrame));
    }

    private async Task OpenModelConfigAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            _logger.LogInformation("Loading model configuration for instrument {Instrument}", _instrument);

            var config = await _modelConfigService.GetConfigurationsAsync(_instrument, cts.Token);
            var supportedModels = await _modelConfigService.GetSupportedModelsAsync(cts.Token);
            var vm = new ModelConfigViewModel(
                _instrument, config, supportedModels, _modelConfigService, _dialogService, _loggerFactory);

            _logger.LogInformation("Model configuration loaded successfully for instrument {Instrument}, " +
                "opening configuration window", _instrument);
            _dialogService.ShowWindow<ModelConfigWindow, ModelConfigViewModel>(vm);
        }
        catch (OperationCanceledException oce)
        {
            _logger.LogWarning(oce, "Loading model configuration for instrument {Instrument} was cancelled", _instrument);
            _dialogService.ShowWarning($"Loading configuration was cancelled: {oce.Message}", "Timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load model configuration for instrument {Instrument}", _instrument);
            _dialogService.ShowError($"Failed to load configuration: {ex.Message}");
        }
    }

    public ICommand ModelConfigCommand { get; }

    public string Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }

    public string Instrument
    {
        get => _instrument;
        set => SetProperty(ref _instrument, value);
    }

    public string Timestamp
    {
        get => _timestamp;
        set => SetProperty(ref _timestamp, value);
    }

    public CandlesSource Candles
    {
        get => _candles;
        set => SetProperty(ref _candles, value);
    }

    public bool IsStreaming
    {
        get => _isStreaming;
        private set => SetProperty(ref _isStreaming, value);
    }

    public async Task StartStreamingAsync()
    {
        if (IsStreaming)
            return; // is this ever hit?

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            _logger.LogInformation("Loading historical data for instrument {Instrument} with a timeout of 10 seconds", _instrument);
            await GetHistoricalCandles(_cancellationTokenSource.Token)
                .WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Loading historical data for instrument {Instrument} timed out", _instrument);
            _dialogService.ShowWarning(
                "Historical data could not be loaded within the timeout period. " +
                "Streaming will continue with live data only.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load historical data for instrument {Instrument}", _instrument);
            _dialogService.ShowWarning(
                $"Failed to load historical data: {ex.Message}. " +
                $"Streaming will continue with live data only.");
        }

        IsStreaming = true;

        using var subscriptionActivity = _activitySource.StartSubscriptionActivity(Instrument);

        try
        {
            var request = new SubscribeRequest();
            request.Instruments.Add(Instrument);

            _logger.LogInformation("Starting price stream for instrument {Instrument}", _instrument);
            using var call = _grpcClient.SubscribeToPrices(request, cancellationToken: _cancellationTokenSource.Token);

            await foreach (var priceUpdate in call.ResponseStream.ReadAllAsync(_cancellationTokenSource.Token))
            {
                using var priceActivity = _activitySource.StartPriceReceivedActivity(priceUpdate.Instrument);
                priceActivity?.SetTag("price.value", priceUpdate.Value);
                priceActivity?.SetTag("price.timestamp", priceUpdate.Timestamp);

                _logger.LogTrace("Received price update for instrument {Instrument}: {Price} at {Timestamp}",
                    _instrument, priceUpdate.Value, new DateTime(priceUpdate.Timestamp));

                // Update UI on the UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Price = priceUpdate.Value.ToString("F2");
                    Timestamp = new DateTime(priceUpdate.Timestamp)
                        .ToString("HH:mm:ss.fff");
                });

                await UpdateCandleChartAsync(priceUpdate);
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            _logger.LogInformation("Price stream for instrument {Instrument} was cancelled", _instrument);
            // Stream was cancelled, this is expected on shutdown
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable || ex.StatusCode == StatusCode.Internal 
            || ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogError(ex, "gRPC error while streaming prices StatusCode=\"{StatusCode}\", Status=\"{Status}\". " +
                "Instrument=\"{Instrument}\": {Message}",
                ex.StatusCode, ex.Status, _instrument, ex.Message);
            _dialogService.ShowError($"Stream error: {ex.StatusCode}. Check server is online, started and configured correctly.", 
                "Streaming Error");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated || ex.StatusCode == StatusCode.PermissionDenied)
        {
            _logger.LogError(ex, "gRPC error while streaming prices StatusCode=\"{StatusCode}\", Status=\"{Status}\". " +
                "Instrument=\"{Instrument}\": {Message}",
                ex.StatusCode, ex.Status, _instrument, ex.Message);
            _dialogService.ShowError($"Stream error: {ex.StatusCode}. Check you have appropriate authorization", "Streaming Error");
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while streaming prices StatusCode=\"{StatusCode}\", Status=\"{Status}\". " +
                "Instrument=\"{Instrument}\": {Message}",
                ex.StatusCode, ex.Status, _instrument, ex.Message);
            _dialogService.ShowError($"Stream error: {ex.StatusCode}.", "Streaming Error");
        }
        catch (Exception ex)
        {
            // Handle other errors
            _logger.LogError(ex, "An error occurred while streaming prices for instrument {Instrument}", _instrument);
            _dialogService.ShowError($"An error occurred while streaming: {ex.Message}", "Streaming Error");
        }
        finally
        {
            IsStreaming = false;
            _logger.LogInformation("Stopped price stream for instrument {Instrument}", _instrument);
        }
    }

    private async Task GetHistoricalCandles(CancellationToken ct)
    {
        // load last N minutes to pre-populate the chart
        var now = DateTime.UtcNow;
        var start = now.AddMinutes(-_loadHistoryOnStartMinutes);

        _logger.LogInformation("Loading historical data for instrument {Instrument} from {Start} to {End}", 
            _instrument, start, now);

        var historicalData = _grpcClient.GetHistoricalData(new HistoricalDataRequest
        {
            Instrument = Instrument,
            StartTimestamp = start.Ticks,
            EndTimestamp = now.Ticks
        }, cancellationToken: ct);

        _logger.LogInformation("Received historical data for instrument {Instrument} with {Count} price points",
            _instrument, historicalData.Prices.Count);

        int candlesCreated = 0;
        PriceUpdate? lastPrice = null;
        using(LogContext.PushProperty("InitializingCandleChart", true))
        {
            foreach (var dataPoint in historicalData.Prices.OrderBy(x => x.Timestamp))
            {
                if (await UpdateCandleChartAsync(dataPoint))
                    candlesCreated++;
                lastPrice = dataPoint;
            }
        }

        _logger.LogInformation("Pre-loaded {CandleCount} candles for instrument {Instrument}",
            candlesCreated, _instrument);

        if (lastPrice != null)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Price = lastPrice.Value.ToString("F2");
                Timestamp = new DateTime(lastPrice.Timestamp)
                    .ToString("HH:mm:ss.fff");
            });
        }
    }

    private async Task<bool> UpdateCandleChartAsync(PriceUpdate priceUpdate, bool logIndividualCandle = true)
    {
        var candle = _candleBuilder.AddPoint(
                            new DateTime(priceUpdate.Timestamp), priceUpdate.Value);

        if (candle is not null)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Candles.Add(new Candle(new DateTime(priceUpdate.Timestamp),
                    candle.Value, _candlePrecision));
            });

            if (logIndividualCandle)
            {
                _logger.LogDebug("Candle completed for instrument {Instrument}: O={Open}, H={High}, L={Low}, C={Close}",
                    _instrument, candle.Value.o, candle.Value.h, candle.Value.l, candle.Value.c);
            }

            return true;
        }

        return false;
    }

    public async Task StopStreamingAsync()
    {
        _logger.LogInformation("Stopping streaming for instrument {Instrument}", _instrument);

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
        IsStreaming = false;
        await Task.CompletedTask;
    }
}
