using System.ComponentModel.DataAnnotations;

namespace MarketData.Client.Shared.Configuration;

public class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public string ServiceName { get; set; } = "MarketData.Client";
    public string ServiceVersion { get; set; } = "1.0.0";
    [Url]
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
}
