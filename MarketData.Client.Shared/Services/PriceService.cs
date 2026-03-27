using Grpc.Core;
using Grpc.Net.Client;
using MarketData.Client.Grpc.Configuration;
using MarketData.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketData.Client.Grpc.Services;

public class PriceService : IPriceService, IDisposable
{
    private readonly ILogger<PriceService> _logger;
    private readonly GrpcChannel _channel;
    private readonly MarketDataService.MarketDataServiceClient _client;

    private bool _disposed;

    public PriceService(IOptions<GrpcSettings> grpcSettings, ILogger<PriceService> logger)
    {
        _logger = logger;
        _channel = GrpcChannel.ForAddress(grpcSettings.Value.ServerUrl);
        _client = new MarketDataService.MarketDataServiceClient(_channel);
    }

    public PriceService(GrpcChannel channel, ILogger<PriceService> logger)
    {
        _logger = logger;
        _channel = channel;
        _client = new MarketDataService.MarketDataServiceClient(_channel);
    }

    public async Task<HistoricalDataResponse> GetHistoricalDataAsync(
        string instrument, long startTimestamp, long endTimestamp, CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting historical data for instrument {Instrument} from {Start} to {End}",
            instrument, new DateTime(startTimestamp, DateTimeKind.Utc), new DateTime(endTimestamp, DateTimeKind.Utc));

        return await _client.GetHistoricalDataAsync(new HistoricalDataRequest
        {
            Instrument = instrument,
            StartTimestamp = startTimestamp,
            EndTimestamp = endTimestamp
        }, cancellationToken: ct);
    }

    public AsyncServerStreamingCall<PriceUpdate> SubscribeToPrices(string instrument, CancellationToken ct = default)
    {
        _logger.LogInformation("Subscribing to price stream for instrument {Instrument}", instrument);
        var request = new SubscribeRequest();
        request.Instruments.Add(instrument);
        return _client.SubscribeToPrices(request, cancellationToken: ct);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _channel?.Dispose();
            }
            _disposed = true;
        }
    }
}
