using MarketData.Client.Grpc.Services;
using MarketData.Client.Grpc.Configuration;
using Microsoft.Extensions.Configuration;
using Serilog;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MarketData.Client;
using MarketData.Client.Grpc;
using System.Text.Json;

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

        try
        {
            LogBanner();
            Log.Information("Starting Console Market Data Client");
            
            var grpcOptions = Options.Create(configuration.GetSection(GrpcSettings.SectionName)
                .Get<GrpcSettings>() ?? new GrpcSettings());

            var modelConfigService = new ModelConfigService(grpcOptions, new LoggerFactory().CreateLogger<ModelConfigService>());
            var priceService = new PriceService(grpcOptions, new LoggerFactory().CreateLogger<PriceService>());

            var priceStreamer = new PriceStreamer(priceService);

            // Initialize gRPC connections to avoid race conditions
            Log.Information("Initializing gRPC connections to {ServerUrl}", grpcOptions.Value.ServerUrl);
            var grpcConnectionInitializer = new GrpcConnectionInitializer(grpcOptions);
            await grpcConnectionInitializer.InitializeAsync();
            Log.Information("gRPC connections ready");

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine($"Press (Ctrl+C) to exit.");
                Console.WriteLine();

                var availableInstruments = (await modelConfigService.GetAllInstrumentsAsync())
                    .Configurations.Select(c => c.InstrumentName);
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
                    Console.Write("Enter instrument name: ");
                    var name = Console.ReadLine();
                    if(string.IsNullOrWhiteSpace(name))
                    {
                        Console.WriteLine("Invalid instrument name");
                        continue;
                    }
                    Console.Write("Enter tick interval (ms): ");
                    if(!int.TryParse(Console.ReadLine(), out var tickIntervalMs))
                    {
                        Console.WriteLine("Invalid tick interval");
                        continue;
                    }
                    Console.Write("Enter initial price: ");
                    if(!double.TryParse(Console.ReadLine(), out var initialPrice))
                    {
                        Console.WriteLine("Invalid initial price");
                        continue;
                    }
                    await modelConfigService.TryAddInstrumentAsync(name, tickIntervalMs, initialPrice);
                }
                else if (input == "2")
                {
                    Console.Write("Enter instrument name: ");
                    var name = Console.ReadLine();
                    if(string.IsNullOrWhiteSpace(name))
                    {
                        Console.WriteLine("Invalid instrument name");
                        continue;
                    }
                    await modelConfigService.TryRemoveInstrumentAsync(name);
                }
                else if (input == "3")
                {
                    var res = await modelConfigService.GetAllInstrumentsAsync();
                    var configs = JsonSerializer.Serialize(res.Configurations);
                    Console.WriteLine(configs);
                }
                else if (input == "4")
                {
                    await priceStreamer.Start();
                }
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
            Log.CloseAndFlush();
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