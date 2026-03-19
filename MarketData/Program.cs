using Microsoft.EntityFrameworkCore;
using MarketData.Data;
using MarketData.Services;
using Scalar.AspNetCore;
using Serilog;

// Configure Serilog early to capture startup logs
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting MarketData service");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddDbContext<MarketDataContext>(options =>
        options.UseSqlite("Data Source=MarketData.db"));

    builder.Services.Configure<MarketDataGeneratorOptions>(
        builder.Configuration.GetSection(MarketDataGeneratorOptions.SectionName));

    builder.Services.AddSingleton<IPriceSimulatorFactory, PriceSimulatorFactory>();
    builder.Services.AddSingleton<IInstrumentModelManager, InstrumentModelManager>();
    builder.Services.AddSingleton<IDefaultModelConfigFactory, DefaultModelConfigFactory>();

    builder.Services.AddHostedService<MarketDataGeneratorService>();

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    builder.Services.AddGrpc();

    var app = builder.Build();

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent);
        };
    });

    if (app.Environment.IsDevelopment())
    {
        // Apply pending migrations at startup in development environment
        //!Important: in production this should be in deployment scripts, not in application code
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();
            context.Database.Migrate();
        }

        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    // Map gRPC services
    app.MapGrpcService<MarketDataGrpcService>();
    app.MapGrpcService<ModelConfigurationGrpcService>();

    Log.Information("MarketData service started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
