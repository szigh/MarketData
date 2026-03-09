using MarketData.Client.Shared.Configuration;
using MarketData.Grpc;

namespace MarketData.Client;

internal class PriceStreamer : GrpcClientBase
{
    private readonly MarketDataService.MarketDataServiceClient _client;

    public PriceStreamer(GrpcSettings settings) : base(settings)
    {
        _client = new MarketDataService.MarketDataServiceClient(_channel);
    }

    public async Task Start()
    {
        Console.Write("Enter instruments to subscribe (comma-separated, e.g., FTSE,AAPL): ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("No instruments specified. Exiting.");
            return;
        }

        var instruments = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var request = new SubscribeRequest();
        foreach (var instrument in instruments)
        {
            request.Instruments.Add(instrument);
        }

        Console.WriteLine($"\nSubscribing to: {string.Join(", ", request.Instruments)}");
        Console.WriteLine("Waiting for price updates... (Press ESC to exit)\n");

        var cts = new CancellationTokenSource();
        _ = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        cts.Cancel();
                        break;
                    }
                }
                Thread.Sleep(100);
            }
        });

        try
        {
            using var call = _client.SubscribeToPrices(request, cancellationToken: cts.Token);

            while (await call.ResponseStream.MoveNext(cts.Token))
            {
                var priceUpdate = call.ResponseStream.Current;
                var timestamp = new DateTime(priceUpdate.Timestamp);
                Console.WriteLine($"[{timestamp:HH:mm:ss.fff}] {priceUpdate.Instrument,-10} {priceUpdate.Value:F4}");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nShutting down...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
            Console.WriteLine($"Make sure the MarketData API is running on {_grpcSettings.ServerUrl}");
        }
    }
}
