using MarketData.Client.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MarketData.Wpf.Client.Extensions;

/// <summary>
/// Extension methods for configuring OpenTelemetry in non-ASP.NET applications.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tracing and metrics instrumentation.
    /// </summary>
    public static IServiceCollection AddOpenTelemetry(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var (serviceName, serviceVersion, _) = GetOpenTelemetryServiceInfo(configuration);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: serviceVersion))
            .WithTracing(tracing => tracing
                .AddHttpClientInstrumentation()
                .AddGrpcClientInstrumentation()
                .AddSource(serviceName)
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(serviceName)
                .AddOtlpExporter());

        return services;
    }

    /// <summary>
    /// Adds OpenTelemetry logging integration to the logging pipeline.
    /// </summary>
    public static ILoggingBuilder AddOpenTelemetryLogging(
        this ILoggingBuilder builder, 
        IConfiguration configuration)
    {
        var (serviceName, serviceVersion, otlpEndpoint) = GetOpenTelemetryServiceInfo(configuration);

        builder.AddOpenTelemetry(otel =>
        {
            otel.SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName, serviceVersion: serviceVersion));
            otel.IncludeFormattedMessage = true;
            otel.IncludeScopes = true;
            otel.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(otlpEndpoint);
            });
        });

        return builder;
    }

    private static (string ServiceName, string ServiceVersion, string OtlpEndpoint) 
        GetOpenTelemetryServiceInfo(IConfiguration configuration)
    {
        var otelOptions = configuration
            .GetSection(OpenTelemetryOptions.SectionName)
            .Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();

        return (otelOptions.ServiceName, otelOptions.ServiceVersion, otelOptions.OtlpEndpoint);
    }
}
