using System.Windows;
using FancyCandles;
using Grpc.Core;
using MarketData.Grpc;

namespace MarketData.Wpf.Client.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly MarketDataService.MarketDataServiceClient _grpcClient;
        private readonly TimeFrame _chartTimeFrame;
        private string _title = "Market Data Client";
        private string _price;
        private string _instrument;
        private string _timestamp;
        private CancellationTokenSource? _cancellationTokenSource;
        private CandlesSource _candles;

        public MainWindowViewModel(MarketDataService.MarketDataServiceClient grpcClient)
        {
            _grpcClient = grpcClient;
            Price = "0.00";
            Instrument = "Unknown";
            Timestamp = string.Empty;

            _chartTimeFrame = TimeFrame.S10;
            Candles = new CandlesSource(_chartTimeFrame);

            // Start streaming automatically
            _ = StartStreamingAsync();
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

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

        private async Task StartStreamingAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var candleBuilder = new CandleBuilder<double>(
                TimeSpan.FromSeconds(_chartTimeFrame.ToSeconds()), true);

            try
            {
                var request = new SubscribeRequest();
                request.Instruments.Add("FTSE");
                //request.Instruments.Add("SNP");

                using var call = _grpcClient.SubscribeToPrices(request, cancellationToken: _cancellationTokenSource.Token);

                await foreach (var priceUpdate in call.ResponseStream.ReadAllAsync(_cancellationTokenSource.Token))
                {
                    // Update UI on the UI thread
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Instrument = priceUpdate.Instrument;
                        Price = priceUpdate.Value.ToString("F2");
                        Timestamp = new DateTime(priceUpdate.Timestamp)
                            .ToString("HH:mm:ss.fff");
                    });

                    var candle = candleBuilder.AddPoint(
                        new DateTime(priceUpdate.Timestamp), priceUpdate.Value);

                    if (candle is not null)
                    {
                        //add candle to UI
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Candles.Add(new Candle
                            {
                                t = new DateTime(priceUpdate.Timestamp),
                                O = double.Round(candle.Value.o, 2),
                                H = double.Round(candle.Value.h, 2),
                                L = double.Round(candle.Value.l, 2),
                                C = double.Round(candle.Value.c, 2),
                                V = candle.Value.count
                            });
                        });
                    }
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
        }

        public async Task StopStreamingAsync()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }
}
