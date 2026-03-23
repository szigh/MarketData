using MarketData.Grpc;
using System.Text.Json;

namespace MarketData.Client.Wpf.ViewModels.AddInstrument.Steps;

public class EndScreen : AddInstrumentViewModelBase
{
    private string _instrumentName = "";
    private string _configurations = "";

    public EndScreen() : base() {}

    public void SetPropertiesAndValidate(string? instrumentName, ConfigurationsResponse configurationsResponse)
    {
        ClearAllErrors();

        if (string.IsNullOrEmpty(instrumentName))
        {
            SetError(nameof(InstrumentName), "Error: Instrument name is null or empty.");
            return;
        }
        InstrumentName = instrumentName;

        if(configurationsResponse == null)
        {
            SetError(nameof(Configurations), "Error: Configurations data is null.");
            return;
        }
        if (string.IsNullOrEmpty(configurationsResponse.ActiveModel))
        {
            SetError(nameof(Configurations), "Error: Active model type is not specified in configurations.");
        }
        if (configurationsResponse.InstrumentName != instrumentName)
        {
            SetError(nameof(InstrumentName), "Error: Instrument name in configurations does not match the provided instrument name.");
        }
        if (configurationsResponse.RandomMultiplicative == null && configurationsResponse.MeanReverting == null &&
            configurationsResponse.FlatConfigured == false && configurationsResponse.RandomAdditiveWalk == null)
        {
            SetError(nameof(Configurations), "Error: No model configuration data found in configurations.");
            return;
        }

        JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
        Configurations = JsonSerializer.Serialize(configurationsResponse, jsonOptions);
    }

    public string InstrumentName
    {
        get => _instrumentName;
        private set => SetProperty(ref _instrumentName, value);
    }

    public string Configurations
    {
        get => _configurations;
        private set => SetProperty(ref _configurations, value);
    }

    protected override void UpdateValidationErrors()
    {
        // Validation is set explicitly via SetPropertiesAndValidate
        // No automatic validation needed for this step
    }
}
