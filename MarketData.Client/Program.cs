using Grpc.Net.Client;
using MarketData.Client.Shared.Configuration;
using MarketData.Grpc;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var grpcSettings = configuration.GetSection(GrpcSettings.SectionName).Get<GrpcSettings>() 
    ?? new GrpcSettings();

Console.WriteLine("Market Data gRPC Client");
Console.WriteLine("======================\n");
Console.WriteLine($"Connecting to: {grpcSettings.ServerUrl}\n");

// Configure the gRPC channel
var channel = GrpcChannel.ForAddress(grpcSettings.ServerUrl);
var client = new MarketDataService.MarketDataServiceClient(channel);

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
Console.WriteLine("Waiting for price updates... (Press Ctrl+C to exit)\n");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    using var call = client.SubscribeToPrices(request, cancellationToken: cts.Token);

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
    Console.WriteLine($"Make sure the MarketData API is running on {grpcSettings.ServerUrl}");
}
