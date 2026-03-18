using MarketData.Configuration;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MarketData.Extensions;

/// <summary>
/// Extension methods for configuring OpenTelemetry (traces, metrics, and logs).
/// </summary>
public static class OpenTelemetryServiceExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tracing instrumentation.
    /// </summary>
    public static WebApplicationBuilder AddOpenTelemetryTracing(this WebApplicationBuilder builder)
    {
        var (serviceName, serviceVersion, _) = GetOpenTelemetryServiceInfo(builder);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: serviceVersion))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.SetTag("http.scheme", request.Scheme);
                    };
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.EnrichWithIDbCommand = (activity, command) =>
                    {
                        activity.SetTag("db.name", "MarketData.db");
                    };
                })
                .AddSource(serviceName)
                //.AddConsoleExporter()
                .AddOtlpExporter());

        return builder;
    }

    /// <summary>
    /// Adds OpenTelemetry metrics instrumentation.
    /// </summary>
    public static WebApplicationBuilder AddOpenTelemetryMetrics(this WebApplicationBuilder builder)
    {
        var (serviceName, serviceVersion, _) = GetOpenTelemetryServiceInfo(builder);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: serviceVersion))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(serviceName)
                //.AddConsoleExporter()
                .AddOtlpExporter());

        return builder;
    }

    /// <summary>
    /// Adds OpenTelemetry logging integration to the logging pipeline.
    /// </summary>
    public static WebApplicationBuilder AddOpenTelemetryLogging(this WebApplicationBuilder builder)
    {
        var (serviceName, serviceVersion, otlpEndpoint) = GetOpenTelemetryServiceInfo(builder);

        builder.Logging.AddOpenTelemetry(otel =>
        {
            otel.SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName, serviceVersion: serviceVersion));
            otel.IncludeFormattedMessage = true;
            otel.IncludeScopes = true;
            //otel.AddConsoleExporter();
            otel.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(otlpEndpoint);
            });
        });

        return builder;
    }

    private static (string ServiceName, string ServiceVersion, string OtlpEndpoint) GetOpenTelemetryServiceInfo(WebApplicationBuilder builder)
    {
        var otelOptions = builder.Configuration
            .GetSection(OpenTelemetryOptions.SectionName)
            .Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();
        return (otelOptions.ServiceName, otelOptions.ServiceVersion, otelOptions.OtlpEndpoint);
    }
}
