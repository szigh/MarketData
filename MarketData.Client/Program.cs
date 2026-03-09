using MarketData.Client;
using MarketData.Client.Shared.Configuration;
using Microsoft.Extensions.Configuration;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var grpcSettings = configuration.GetSection(GrpcSettings.SectionName)
            .Get<GrpcSettings>() ?? new GrpcSettings();

        var modelConfigClient = new GrpcModelConfigClient(grpcSettings);
        var priceStreamer = new PriceStreamer(grpcSettings);

        while (true)
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
            if(input == "1")
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
    }
}