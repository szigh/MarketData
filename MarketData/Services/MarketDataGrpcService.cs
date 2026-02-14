using Grpc.Core;
using MarketData.Grpc;
using System.Threading.Channels;

namespace MarketData.Services;

public class MarketDataGrpcService : MarketDataService.MarketDataServiceBase
{
    private readonly ILogger<MarketDataGrpcService> _logger;
    private static readonly Channel<PriceUpdate> _priceChannel = 
        Channel.CreateUnbounded<PriceUpdate>();

    public MarketDataGrpcService(ILogger<MarketDataGrpcService> logger)
    {
        _logger = logger;
    }

    public override async Task SubscribeToPrices(
        SubscribeRequest request,
        IServerStreamWriter<PriceUpdate> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation("Client subscribed to: {Instruments}", 
            string.Join(", ", request.Instruments));

        var reader = _priceChannel.Reader;

        try
        {
            while (await reader.WaitToReadAsync(context.CancellationToken))
            {
                while (reader.TryRead(out var priceUpdate))
                {
                    if (request.Instruments.Contains(priceUpdate.Instrument))
                    {
                        await responseStream.WriteAsync(priceUpdate, context.CancellationToken);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client disconnected");
        }

        return;
    }

    public static async Task BroadcastPrice(string instrument, decimal value, DateTime timestamp)
    {
        var update = new PriceUpdate
        {
            Instrument = instrument,
            Value = (double)value,
            Timestamp = timestamp.Ticks
        };

        await _priceChannel.Writer.WriteAsync(update);
    }
}
