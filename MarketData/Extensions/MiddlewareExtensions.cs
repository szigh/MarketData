using MarketData.Services;
using Scalar.AspNetCore;
using Serilog;

namespace MarketData.Extensions;

/// <summary>
/// Extension methods for configuring middleware and endpoints.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds Serilog HTTP request logging middleware.
    /// </summary>
    public static IApplicationBuilder UseSerilogHttpRequestLogging(this IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent);
            };
        });

        return app;
    }

    /// <summary>
    /// Configures development-specific features (database migrations).
    /// </summary>
    public static IApplicationBuilder UseDevelopmentFeatures(
        this IApplicationBuilder app,
        IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            // Apply pending migrations at startup
            app.ApplyDatabaseMigrations();
        }

        return app;
    }

    /// <summary>
    /// Maps application endpoints (controllers, gRPC services, and dev tools).
    /// </summary>
    public static IEndpointRouteBuilder MapApplicationEndpoints(
        this IEndpointRouteBuilder endpoints,
        IWebHostEnvironment environment)
    {
        endpoints.MapControllers();

        endpoints.MapGrpcService<MarketDataGrpcService>();
        endpoints.MapGrpcService<ModelConfigurationGrpcService>();

        // Development-only endpoints
        if (environment.IsDevelopment())
        {
            endpoints.MapOpenApi();
            endpoints.MapScalarApiReference();
        }

        return endpoints;
    }
}
