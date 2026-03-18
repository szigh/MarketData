using System.ComponentModel.DataAnnotations;

namespace MarketData.Configuration;

public class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public string ServiceName { get; set; } = "MarketData.Server";
    public string ServiceVersion { get; set; } = "1.0.0";
    [Url]
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
}
