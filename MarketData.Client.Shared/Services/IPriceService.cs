using Grpc.Core;
using MarketData.Grpc;

namespace MarketData.Client.Grpc.Services;

/// <summary>
/// Service interface for streaming live prices and retrieving historical price data via gRPC
/// </summary>
public interface IPriceService : IDisposable
{
    /// <summary>
    /// Retrieves historical price data for a given instrument within a timestamp range
    /// </summary>
    Task<HistoricalDataResponse> GetHistoricalDataAsync(
        string instrument, long startTimestamp, long endTimestamp, CancellationToken ct = default);

    /// <summary>
    /// Opens a server-streaming call to subscribe to live price updates for an instrument
    /// </summary>
    AsyncServerStreamingCall<PriceUpdate> SubscribeToPrices(string instrument, CancellationToken ct = default);
}
