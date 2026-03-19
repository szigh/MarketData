using Serilog.Core;
using Serilog.Events;
using System.Reflection;

namespace MarketData.Logging;

/// <summary>
/// Enriches all log events with MarketData service metadata
/// </summary>
public class MarketDataEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var version = Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString() ?? "unknown";
        
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("ServiceName", "MarketData"));
        
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("ServiceVersion", version));
        
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("MachineName", Environment.MachineName));
    }
}
