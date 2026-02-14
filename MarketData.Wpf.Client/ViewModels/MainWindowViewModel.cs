using System.Windows;
using Grpc.Core;
using MarketData.Grpc;

namespace MarketData.Wpf.Client.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly MarketDataService.MarketDataServiceClient _grpcClient;
        private string _title = "Market Data Client";
        private string _price;
        private string _instrument;
        private string _timestamp;
        private CancellationTokenSource? _cancellationTokenSource;

        public MainWindowViewModel(MarketDataService.MarketDataServiceClient grpcClient)
        {
            _grpcClient = grpcClient;
            Price = "0.00";
            Instrument = "Unknown";
            Timestamp = string.Empty;

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

        private async Task StartStreamingAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                var request = new SubscribeRequest();
                // Subscribe to all instruments - modify as needed
                request.Instruments.Add("FTSE");
                request.Instruments.Add("SNP");

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
