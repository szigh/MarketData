using System.Text.RegularExpressions;

namespace MarketData.Client.Wpf.ViewModels.AddInstrument.Steps;

public class NameInstrument : AddInstrumentViewModelBase
{
    private string _instrumentName = "";
    private string _validationMessage = "";

    #region Default values for new instrument
    private double _initialPrice = 100d;
    private string _selectedModel = "Flat";
    private int _tickIntervalMs = 60000;
    #endregion

    private readonly string[] _availableModels;
    private readonly HashSet<string> _existingInstruments;

    public NameInstrument(IEnumerable<string> availableModels, IEnumerable<string> existingInstruments)
    {
        _availableModels = availableModels.ToArray();
        _existingInstruments = existingInstruments.ToHashSet(StringComparer.OrdinalIgnoreCase);

        PropertyChanged += (_, _) => UpdateValidationMessage();
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

    public override string ValidationMessage
    {
        get => _validationMessage;
        protected set => SetProperty(ref _validationMessage, value);
    }

    protected override void UpdateValidationMessage()
    {
        if (string.IsNullOrWhiteSpace(InstrumentName))
        {
            ValidationMessage = "Instrument name cannot be empty.";
        }
        else if (!Regex.IsMatch(InstrumentName, "^[a-zA-Z0-9]+$"))
        {
            ValidationMessage = "Instrument name must contain only alphanumeric characters.";
        }
        else if (_existingInstruments.Contains(InstrumentName))
        {
            ValidationMessage = "An instrument with this name already exists.";
        }
        else if (InitialPrice <= 0)
        {
            ValidationMessage = "Initial price must be greater than zero.";
        }
        else if (!_availableModels.Contains(SelectedModel))
        {
            ValidationMessage = "Selected model is not valid.";
        }
        else if (TickIntervalMs <= 0)
        {
            ValidationMessage = "Tick interval must be greater than zero.";
        }
        else
        {
            ValidationMessage = "";
        }
    }
}
