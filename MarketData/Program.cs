using MarketData.Extensions;
using Serilog;

// Configure Serilog early to capture startup logs
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting MarketData service");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Observability
    builder.AddOpenTelemetryTracing();
    builder.AddOpenTelemetryMetrics();
    builder.AddOpenTelemetryLogging();
    builder.AddCustomTelemetry();
    builder.AddSerilogLogging();

    // Infrastructure
    builder.AddMarketDataDatabase();

    // Application Services
    builder.AddMarketDataServices();
    builder.AddWebApiServices();

    var app = builder.Build();

    // Configure middleware pipeline
    app.UseSerilogHttpRequestLogging();
    app.UseDevelopmentFeatures(app.Environment);
    app.UseHttpsRedirection();
    app.UseAuthorization();

    // Configure endpoints
    app.MapApplicationEndpoints(app.Environment);

    Log.Information("MarketData service started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
