using MarketData.Wpf.Shared;

namespace MarketData.Wpf.Client.ViewModels;

public class InstrumentTabViewModel : ViewModelBase
{
    private InstrumentViewModel _instrumentViewModel;

    public InstrumentTabViewModel(InstrumentViewModel instrumentViewModel)
    {
        _instrumentViewModel = instrumentViewModel;
    }

    public InstrumentViewModel InstrumentViewModel
    {
        get => _instrumentViewModel;
        set => SetProperty(ref _instrumentViewModel, value);
    }

    public string Header => InstrumentViewModel.Instrument;
}
