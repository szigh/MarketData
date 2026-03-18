using MarketData.Client;
using MarketData.Client.Configuration;
using MarketData.Client.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        TracerProvider? tracerProvider = null;
        MeterProvider? meterProvider = null;
        ILoggerFactory? loggerFactory = null;

        try
        {
            LogBanner();
            Log.Information("Starting Console Market Data Client");

            // Get OpenTelemetry configuration
            var otelOptions = configuration
                .GetSection(OpenTelemetryOptions.SectionName)
                .Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();

            // Configure OpenTelemetry using Sdk static methods
            tracerProvider = Sdk.CreateTracerProviderBuilder()
                .ConfigureResource(resource => resource
                    .AddService(otelOptions.ServiceName, serviceVersion: otelOptions.ServiceVersion))
                .AddHttpClientInstrumentation()
                .AddGrpcClientInstrumentation()
                .AddSource(otelOptions.ServiceName)
                //.AddConsoleExporter()
                .AddOtlpExporter()
                .Build();

            meterProvider = Sdk.CreateMeterProviderBuilder()
                .ConfigureResource(resource => resource
                    .AddService(otelOptions.ServiceName, serviceVersion: otelOptions.ServiceVersion))
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(otelOptions.ServiceName)
                //.AddConsoleExporter()
                .AddOtlpExporter()
                .Build();

            // Configure OpenTelemetry Logging using LoggerFactory
            loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(Log.Logger);
                builder.AddOpenTelemetry(logging =>
                {
                    logging.SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(otelOptions.ServiceName, serviceVersion: otelOptions.ServiceVersion));
                    logging.IncludeFormattedMessage = true;
                    logging.IncludeScopes = true;
                    logging.AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(otelOptions.OtlpEndpoint);
                    });
                });
            });

            var grpcSettings = configuration.GetSection(GrpcSettings.SectionName)
                .Get<GrpcSettings>() ?? new GrpcSettings();

            var modelConfigClient = new GrpcModelConfigClient(grpcSettings);
            var priceStreamer = new PriceStreamer(grpcSettings);

            // Initialize gRPC connections to avoid race conditions
            Log.Information("Initializing gRPC connections to {ServerUrl}", grpcSettings.ServerUrl);
            await modelConfigClient.InitializeAsync();
            await priceStreamer.InitializeAsync();
            Log.Information("gRPC connections ready");

            while (true)
            {
                await RunConsoleMenu(modelConfigClient, priceStreamer);
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.Information("Shutting down Console Market Data Client");

            // Dispose OpenTelemetry providers
            tracerProvider?.Dispose();
            meterProvider?.Dispose();
            loggerFactory?.Dispose();

            Log.CloseAndFlush();
        }
    }

    private static async Task RunConsoleMenu(GrpcModelConfigClient modelConfigClient, PriceStreamer priceStreamer)
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine($"Press (Ctrl+C) to exit.");
        Console.WriteLine();

        var availableInstruments = await modelConfigClient.GetConfiguredInstruments();
        Console.WriteLine($"Available instruments: {string.Join(", ", availableInstruments)}");

        var sep = new string('=', 30);
        Console.WriteLine($"Menu {sep}");
        Console.WriteLine($"1. Add instrument");
        Console.WriteLine($"2. Remove instrument");
        Console.WriteLine($"3. View configurations");
        Console.WriteLine($"4. Start price streaming");

        Console.Write($">>> ");
        var input = Console.ReadLine();
        if (input == "1")
        {
            await modelConfigClient.AddInstrument();
        }
        else if (input == "2")
        {
            await modelConfigClient.RemoveInstrument();
        }
        else if (input == "3")
        {
            await modelConfigClient.GetConfiguredInstruments(printConfigs: true);
        }
        else if (input == "4")
        {
            await priceStreamer.Start();
        }
    }

    private static void LogBanner()
    {
        const string banner =
@"
          __  __            _        _       _       _        
         |  \/  |          | |      | |     | |     | |       
         | \  / | __ _ _ __| | _____| |_  __| | __ _| |_ __ _ 
         | |\/| |/ _` | '__| |/ / _ \ __|/ _` |/ _` | __/ _` |
         | |  | | (_| | |  |   <  __/ |_| (_| | (_| | || (_| |
         |_|  |_|\__,_|_|  |_|\_\___|\__|\__,_|\__,_|\__\__,_|
           ____                      _        
          / ___|___  _ __  ___  ___ | | ___   
         | |   / _ \| '_ \/ __|/ _ \| |/ _ \  
         | |__| (_) | | | \__ \ (_) | |  __/  
          \____\___/|_| |_|___/\___/|_|\___|  
                                              ";
        Log.Logger.Information(banner);
    }
}