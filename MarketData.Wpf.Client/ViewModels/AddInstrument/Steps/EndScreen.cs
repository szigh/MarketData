using MarketData.Grpc;
using MarketData.Wpf.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MarketData.Client.Wpf.ViewModels.AddInstrument.Steps
{
    public class EndScreen : AddInstrumentViewModelBase
    {
        private string _validationMessage = "";
        private string _instrumentName = "";
        private string _configurations = "";

        public EndScreen(){}

        public void SetPropertiesAndValidate(string? instrumentName, ConfigurationsResponse configurationsResponse)
        {
            if (string.IsNullOrEmpty(instrumentName))
            {
                ValidationMessage = "Error: Instrument name is null or empty.";
                return;
            }
            InstrumentName = instrumentName;

            if(configurationsResponse == null)
            {
                ValidationMessage = "Error: Configurations data is null.";
                return;
            }
            if (string.IsNullOrEmpty(configurationsResponse.ActiveModel))
            {
                ValidationMessage = "Error: Active model type is not specified in configurations.";
                return;
            }
            if (configurationsResponse.InstrumentName != instrumentName)
            {
                ValidationMessage = "Error: Instrument name in configurations does not match the provided instrument name.";
                return;
            }
            if (configurationsResponse.RandomMultiplicative == null && configurationsResponse.MeanReverting == null &&
                configurationsResponse.FlatConfigured == false && configurationsResponse.RandomAdditiveWalk == null)
            {
                ValidationMessage = "Error: No model configuration data found in configurations.";
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

        public override string ValidationMessage
        {
            get => _validationMessage;
            protected set => SetProperty(ref _validationMessage, value);
        }

        protected override void UpdateValidationMessage()
        {
            //validation set in ctor only
        }
    }
}
