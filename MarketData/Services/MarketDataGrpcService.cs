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
    private static readonly Channel<PriceUpdate> _priceChannel = 
        Channel.CreateUnbounded<PriceUpdate>();

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
