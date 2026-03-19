using Grpc.Core;
using MarketData.Grpc;
using MarketData.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace MarketData.Services;

public class MarketDataGrpcService : MarketDataService.MarketDataServiceBase
{
    private readonly ILogger<MarketDataGrpcService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly List<Channel<PriceUpdate>> _subscriberChannels = new();
    private static readonly Lock _subscribersLock = new();

    public MarketDataGrpcService(
        ILogger<MarketDataGrpcService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public override async Task SubscribeToPrices(
        SubscribeRequest request,
        IServerStreamWriter<PriceUpdate> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation("Client subscribed to: {Instruments}", 
            string.Join(", ", request.Instruments));

        var subscriberChannel = Channel.CreateUnbounded<PriceUpdate>();

        lock (_subscribersLock)
        {
            _subscriberChannels.Add(subscriberChannel);
        }

        try
        {
            await foreach (var priceUpdate in subscriberChannel.Reader.ReadAllAsync(context.CancellationToken))
            {
                if (request.Instruments.Contains(priceUpdate.Instrument))
                {
                    _logger.LogTrace("Writing price update to stream for {Instrument}: {Value} at {Timestamp}",
                        priceUpdate.Instrument,
                        priceUpdate.Value,
                        new DateTime(priceUpdate.Timestamp));
                    await responseStream.WriteAsync(priceUpdate, context.CancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client disconnected");
        }
        finally
        {
            lock (_subscribersLock)
            {
                subscriberChannel.Writer.TryComplete();
                _subscriberChannels.Remove(subscriberChannel);
            }
        }
    }

    public override async Task<HistoricalDataResponse> GetHistoricalData(
        HistoricalDataRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("Historical data request for {Instrument} from {Start} to {End}",
            request.Instrument,
            new DateTime(request.StartTimestamp),
            new DateTime(request.EndTimestamp));

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var startDate = new DateTime(request.StartTimestamp);
        var endDate = new DateTime(request.EndTimestamp);

        var prices = await dbContext.Prices
            .Where(p => p.Instrument == request.Instrument &&
                        p.Timestamp >= startDate &&
                        p.Timestamp <= endDate)
            .OrderBy(p => p.Timestamp)
            .Select(p => new PriceUpdate
            {
                Instrument = p.Instrument!,
                Value = (double)p.Value,
                Timestamp = p.Timestamp.Ticks
            })
            .ToListAsync(context.CancellationToken);

        var response = new HistoricalDataResponse();
        response.Prices.AddRange(prices);

        _logger.LogInformation("Returning {Count} historical prices", prices.Count);

        return response;
    }

    public static async Task BroadcastPrice(string instrument, decimal value, DateTime timestamp, 
        CancellationToken ct = default)
    {
        var update = new PriceUpdate
        {
            Instrument = instrument,
            Value = (double)value,
            Timestamp = timestamp.Ticks
        };

        List<Channel<PriceUpdate>> channelsCopy;
        lock (_subscribersLock)
        {
            channelsCopy = new List<Channel<PriceUpdate>>(_subscriberChannels);
        }

        foreach (var channel in channelsCopy)
        {
            await channel.Writer.WriteAsync(update, ct);
        }
    }
}
