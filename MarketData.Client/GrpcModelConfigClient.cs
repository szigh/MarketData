using MarketData.Client.Shared.Configuration;
using MarketData.Grpc;
using System.Text.Json;

namespace MarketData.Client;

internal class GrpcModelConfigClient : GrpcClientBase
{
    private readonly ModelConfigurationService.ModelConfigurationServiceClient _client;

    public GrpcModelConfigClient(GrpcSettings settings) : base(settings)
    {
        _client = new ModelConfigurationService.ModelConfigurationServiceClient(_channel);
    }

    public async Task<IEnumerable<string>> GetConfiguredInstruments(bool printConfigs = false)
    {
        var result = await _client.GetAllInstrumentsAsync(new GetAllInstrumentsRequest());
        if (printConfigs)
        {
            result.Configurations.ToList().ForEach(config =>
            {
                Console.WriteLine($"Instrument: {config.InstrumentName}");
                var configAsString = JsonSerializer.Serialize(config);
                Console.WriteLine("Config:");
                Console.WriteLine(configAsString);
            });
        }
        return result.Configurations.Select(c => c.InstrumentName);
    }

    public async Task AddInstrument()
    {
        Console.WriteLine("Adding instrument");
        Console.Write($"Enter instrument name: ");
        var name = Console.ReadLine();
        Console.Write($"Enter tick interval (ms) (int): ");
        var tickInterval = int.Parse(Console.ReadLine() ?? "10000");
        Console.Write($"Enter initial price (double): ");
        var initialPrice = double.Parse(Console.ReadLine() ?? "100");

        var response = await _client.TryAddInstrumentAsync(new TryAddInstrumentRequest
        {
            InstrumentName = name,
            TickIntervalMs = tickInterval,
            InitialPriceValue = initialPrice,
        });

        if (response.Added)
            Console.WriteLine($"Instrument {name} added successfully:\r\n{response.Message}");
        else
            Console.WriteLine($"Failed to add instrument {name}. Reason: {response.Message}");
    }

    public async Task RemoveInstrument()
    {
        Console.WriteLine("Removing instrument");
        Console.Write($"Enter instrument name: ");
        var name = Console.ReadLine();
        var response = await _client.TryRemoveInstrumentAsync(new TryRemoveInstrumentRequest
        {
            InstrumentName = name,
        });
        if (response.Removed)
            Console.WriteLine($"Instrument {name} removed successfully:\r\n{response.Message}");
        else
            Console.WriteLine($"Failed to remove instrument {name}. Reason: {response.Message}");
    }
}
