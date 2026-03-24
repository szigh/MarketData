using MarketData.Grpc;
using System.Text.Json;

namespace MarketData.Client.Wpf.ViewModels.AddInstrument.Steps;

public class EndScreen : AddInstrumentViewModelBase
{
    private string _instrumentName = "";
    private string _configurations = "";
    private ConfigurationsResponse _configurationsResponse = new();

    public EndScreen() : base() {}

    public void SetPropertiesAndValidate(string? instrumentName, ConfigurationsResponse configurationsResponse)
    {
        InstrumentName = instrumentName ?? string.Empty;
        _configurationsResponse = configurationsResponse;

        JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
        Configurations = JsonSerializer.Serialize(configurationsResponse, jsonOptions);

        UpdateValidationErrors();
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
        ClearAllErrors();

        if (string.IsNullOrEmpty(InstrumentName))
        {
            SetError(nameof(InstrumentName), "Error: Instrument name is null or empty.");
        }

        if (string.IsNullOrEmpty(_configurationsResponse.ActiveModel))
        {
            SetError(nameof(_configurationsResponse), "Error: Active model type is not specified in configurations.");
        }
        if (_configurationsResponse.InstrumentName != InstrumentName)
        {
            SetError(nameof(InstrumentName), "Error: Instrument name in configurations does not match the provided instrument name.");
        }
        if (_configurationsResponse.RandomMultiplicative == null && _configurationsResponse.MeanReverting == null &&
            _configurationsResponse.FlatConfigured == false && _configurationsResponse.RandomAdditiveWalk == null)
        {
            SetError(nameof(_configurationsResponse), "Error: No model configuration data found in configurations.");
        }
    }
}
