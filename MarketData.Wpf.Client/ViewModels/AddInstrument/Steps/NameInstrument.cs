using System.Text.RegularExpressions;

namespace MarketData.Client.Wpf.ViewModels.AddInstrument.Steps;

public class NameInstrument : AddInstrumentViewModelBase
{
    private string _instrumentName = "";

    #region Default values for new instrument
    private double _initialPrice = 100d;
    private string _selectedModel = "Flat";
    private int _tickIntervalMs = 60000;
    #endregion

    private readonly string[] _availableModels;
    private readonly HashSet<string> _existingInstruments;

    public NameInstrument(IEnumerable<string> availableModels, IEnumerable<string> existingInstruments) : base()
    {
        _availableModels = availableModels.ToArray();
        _existingInstruments = existingInstruments.ToHashSet(StringComparer.OrdinalIgnoreCase);

        UpdateValidationErrors();
    }

    public string InstrumentName
    {
        get => _instrumentName;
        set => SetProperty(ref _instrumentName, value);
    }

    public double InitialPrice
    {
        get => _initialPrice;
        set => SetProperty(ref _initialPrice, value);
    }

    public string SelectedModel
    {
        get => _selectedModel;
        set => SetProperty(ref _selectedModel, value);
    }

    public int TickIntervalMs
    {
        get => _tickIntervalMs;
        set => SetProperty(ref _tickIntervalMs, value);
    }

    public string[] AvailableModels => _availableModels;

    protected override void UpdateValidationErrors()
    {
        // Clear all previous errors
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(InstrumentName))
        {
            SetError(nameof(InstrumentName), "Instrument name cannot be empty.");
        }
        else if (!Regex.IsMatch(InstrumentName, "^[a-zA-Z0-9]+$"))
        {
            SetError(nameof(InstrumentName), "Instrument name must contain only alphanumeric characters.");
        }
        else if (_existingInstruments.Contains(InstrumentName))
        {
            SetError(nameof(InstrumentName), "An instrument with this name already exists.");
        }
        if (InitialPrice <= 0)
        {
            SetError(nameof(InitialPrice), "Initial price must be greater than zero.");
        }
        if (InitialPrice > double.MaxValue)
        {
            SetError(nameof(InitialPrice), $"Initial price must be less than or equal to {double.MaxValue}.");
        }
        if (double.IsNaN(InitialPrice) || double.IsInfinity(InitialPrice))
        {
            SetError(nameof(InitialPrice), "Initial price must be a finite number.");
        }
        if (!_availableModels.Contains(SelectedModel))
        {
            SetError(nameof(SelectedModel), "Selected model is not valid.");
        }
        if (TickIntervalMs <= 0)
        {
            SetError(nameof(TickIntervalMs), "Tick interval must be greater than zero.");
        }
    }
}
