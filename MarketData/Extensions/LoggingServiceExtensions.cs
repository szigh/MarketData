using Serilog;
using Serilog.Extensions.Hosting;

namespace MarketData.Extensions;

/// <summary>
/// Extension methods for configuring Serilog logging.
/// </summary>
public static class LoggingServiceExtensions
{
    /// <summary>
    /// Adds Serilog as a logging provider without replacing the ILogger infrastructure.
    /// This allows Serilog to work alongside OpenTelemetry and other logging providers.
    /// </summary>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        // Create Serilog logger from configuration
        var serilogLogger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        // Register DiagnosticContext for UseSerilogRequestLogging middleware
        builder.Services.AddSingleton<DiagnosticContext>(_ => new DiagnosticContext(serilogLogger));

        // Add Serilog as a logging provider (works with ILogger + OpenTelemetry)
        builder.Logging.AddSerilog(serilogLogger);

        return builder;
    }
}
