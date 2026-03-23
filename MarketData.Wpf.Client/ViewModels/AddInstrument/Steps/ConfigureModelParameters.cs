using MarketData.Wpf.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace MarketData.Client.Wpf.ViewModels.AddInstrument.Steps
{
    public class ConfigureModelParameters : AddInstrumentViewModelBase
    {
        private string _validationMessage;

        protected override void UpdateValidationMessage()
        {
            throw new NotImplementedException();
        }


        public override string ValidationMessage
        {
            get => _validationMessage;
            protected set => SetProperty(ref _validationMessage, value);
        }
    }
}
