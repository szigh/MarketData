using MarketData.Grpc;
using MarketData.Wpf.Client.Services;
using MarketData.Wpf.Shared;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace MarketData.Wpf.Client.ViewModels.ModelConfigs;

public class RandomAdditiveWalkConfigViewModel : ModelConfigViewModelBase
{
    private readonly IModelConfigService _modelConfigService;
    private string _validationMessage = string.Empty;

    public RandomAdditiveWalkConfigViewModel(string instrumentName, RandomAdditiveWalkConfigData config,
        IModelConfigService modelConfigService)
        : base(instrumentName)
    {
        _modelConfigService = modelConfigService;
        WalkSteps = new ObservableCollection<WalkStepViewModel>(
            config.WalkSteps.Select(s => new WalkStepViewModel(s.Probability, s.StepValue))
        );

        WalkSteps.CollectionChanged += OnWalkStepsCollectionChanged;
        foreach (var step in WalkSteps)
            step.PropertyChanged += OnStepPropertyChanged;

        AddStepCommand = new RelayCommand(AddStep);
        RemoveStepCommand = new RelayCommand<WalkStepViewModel>(RemoveStep, CanRemoveStep);

        ValidateProbabilities();
    }

    public ObservableCollection<WalkStepViewModel> WalkSteps { get; }
    public int StepCount => WalkSteps.Count;

    public RelayCommand AddStepCommand { get; }
    public RelayCommand<WalkStepViewModel> RemoveStepCommand { get; }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public bool IsValid => string.IsNullOrEmpty(ValidationMessage);

    private void AddStep()
    {
        var newStep = new WalkStepViewModel(0.0, 0.0);
        newStep.PropertyChanged += OnStepPropertyChanged;
        WalkSteps.Add(newStep);
        IsModified = true;
        ValidateProbabilities();
    }

    private void RemoveStep(WalkStepViewModel? step)
    {
        if (step != null && WalkSteps.Contains(step))
        {
            step.PropertyChanged -= OnStepPropertyChanged;
            WalkSteps.Remove(step);
            IsModified = true;
            ValidateProbabilities();
        }
    }

    private bool CanRemoveStep(WalkStepViewModel? step) => WalkSteps.Count > 1;

    private void OnWalkStepsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(StepCount));
        RemoveStepCommand.RaiseCanExecuteChanged();
    }

    private void OnStepPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        IsModified = true;
        ValidateProbabilities();
    }

    private void ValidateProbabilities()
    {
        // Check if all probabilities are in range [0, 1]
        foreach (var step in WalkSteps)
        {
            if (step.Probability < 0 || step.Probability > 1)
            {
                ValidationMessage = $"All probabilities must be between 0 and 1.";
                OnPropertyChanged(nameof(IsValid));
                return;
            }
        }

        // Check if probabilities sum to 1 (with small tolerance for floating point errors)
        double sum = WalkSteps.Sum(s => s.Probability);
        const double tolerance = 0.0001;

        if (Math.Abs(sum - 1.0) > tolerance)
        {
            ValidationMessage = $"Probabilities must sum to 1.0 (current sum: {sum:F4}).";
            OnPropertyChanged(nameof(IsValid));
            return;
        }

        ValidationMessage = string.Empty;
        OnPropertyChanged(nameof(IsValid));
    }

    protected override async Task<bool> TryExecutePublishConfigChangesAsync()
    {
        if (!IsValid)
        {
            return false;
        }

        await _modelConfigService.UpdateRandomAdditiveWalkConfigAsync(
            InstrumentName,
            WalkSteps.Select(s => (s.Probability, s.StepValue)).ToList());

        return true;
    }
}

public class WalkStepViewModel : ViewModelBase
{
    private double _probability;
    private double _stepValue;

    public WalkStepViewModel(double probability, double stepValue)
    {
        _probability = probability;
        _stepValue = stepValue;
    }

    public double Probability
    {
        get => _probability;
        set
        {
            SetProperty(ref _probability, value);
            OnPropertyChanged(nameof(ProbabilityPercentage));
        }
    }

    public double StepValue
    {
        get => _stepValue;
        set => SetProperty(ref _stepValue, value);
    }

    public string ProbabilityPercentage => $"{Probability * 100:F1}%";
}
