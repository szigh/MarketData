using System.Windows;
using System.Windows.Input;
using FancyCandles;
using Grpc.Core;
using MarketData.Grpc;
using MarketData.Wpf.Client.FancyCandlesImplementations;
using MarketData.Wpf.Client.Services;
using MarketData.Wpf.Client.Views;
using MarketData.Wpf.Shared;

namespace MarketData.Wpf.Client.ViewModels;

public class InstrumentViewModel : ViewModelBase
{
    private readonly MarketDataService.MarketDataServiceClient _grpcClient;
    private readonly ModelConfigService _modelConfigService;
    private const TimeFrame _chartTimeFrame = TimeFrame.S10;
    private readonly CandleBuilder<double> _candleBuilder;
    private const int _candlePrecision = 2;

    private string _price;
    private string _instrument;
    private string _timestamp;
    private CancellationTokenSource? _cancellationTokenSource;
    private CandlesSource _candles;
    private bool _isStreaming;

    public InstrumentViewModel(MarketDataService.MarketDataServiceClient grpcClient, string instrumentName)
    {
        _grpcClient = grpcClient;
        _instrument = instrumentName;
        Price = "#.##";
        Timestamp = string.Empty;

        //TODO use config for server address
        _modelConfigService = new ModelConfigService("https://localhost:7264");

        ModelConfigCommand = new AsyncRelayCommand(OpenModelConfigAsync);

        _candleBuilder = new CandleBuilder<double>(
            TimeSpan.FromSeconds(_chartTimeFrame.ToSeconds()), true);
        Candles = new CandlesSource(_chartTimeFrame);
    }

    private async Task OpenModelConfigAsync()
    {
        try
        {
            var config = await _modelConfigService.GetConfigurationsAsync(_instrument);
            var vm = new ModelConfigViewModel(_instrument, _modelConfigService, config);
            var view = new ModelConfigWindow(vm);
            view.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load configuration: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
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

    private async Task GetHistoricalCandles()
    {
        //load last 1d to pre-populate the chart
        var now = DateTime.UtcNow;
        var start = now.AddDays(-1);
        var historicalData = _grpcClient.GetHistoricalData(new HistoricalDataRequest
        {
            Instrument = Instrument,
            StartTimestamp = start.Ticks,
            EndTimestamp = now.Ticks
        });
        foreach (var dataPoint in historicalData.Prices.OrderBy(x => x.Timestamp))
        {
            await UpdateCandleChartAsync(dataPoint, false);
        }
    }

    public async Task StartStreamingAsync()
    {
        if (IsStreaming)
            return; // is this ever hit?

        await GetHistoricalCandles();

        _cancellationTokenSource = new CancellationTokenSource();
        IsStreaming = true;

        try
        {
            var request = new SubscribeRequest();
            request.Instruments.Add(Instrument);

            using var call = _grpcClient.SubscribeToPrices(request, cancellationToken: _cancellationTokenSource.Token);

            await foreach (var priceUpdate in call.ResponseStream.ReadAllAsync(_cancellationTokenSource.Token))
            {
                // Update UI on the UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Price = priceUpdate.Value.ToString("F2");
                    Timestamp = new DateTime(priceUpdate.Timestamp)
                        .ToString("HH:mm:ss.fff");
                });

                await UpdateCandleChartAsync(priceUpdate, true);
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            // Stream was cancelled, this is expected on shutdown
        }
        catch (Exception ex)
        {
            // Handle other errors
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Price = $"Error: {ex.Message}";
            });
        }
        finally
        {
            IsStreaming = false;
        }
    }

    private async Task UpdateCandleChartAsync(PriceUpdate priceUpdate, bool updateLastCandle)
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
        }
    }

    public async Task StopStreamingAsync()
    {
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
