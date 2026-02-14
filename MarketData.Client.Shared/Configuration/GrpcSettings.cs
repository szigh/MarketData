namespace MarketData.Client.Shared.Configuration;

/// <summary>
/// Configuration settings for gRPC client connections.
/// Bind this to the "GrpcSettings" section in appsettings.json.
/// </summary>
/// <example>
/// Example appsettings.json:
/// <code>
/// {
///   "GrpcSettings": {
///     "ServerUrl": "https://localhost:7264"
///   }
/// }
/// </code>
/// </example>
public class GrpcSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "GrpcSettings";

    /// <summary>
    /// The URL of the gRPC server.
    /// Default value is "https://localhost:7264"
    /// </summary>
    public string ServerUrl { get; set; } = "https://localhost:7264";
}
