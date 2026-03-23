using MarketData.Wpf.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace MarketData.Client.Wpf.ViewModels.AddInstrument.Steps
{
    public class EndScreen : AddInstrumentViewModelBase
    {
        private string _validationMessage;

        public override string ValidationMessage
        {
            get => _validationMessage;
            protected set => SetProperty(ref _validationMessage, value);
        }

        protected override void UpdateValidationMessage()
        {
            throw new NotImplementedException();
        }
    }
}
