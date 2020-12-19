using System;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples.HttpTransfers
{
    public class HttpTransferViewModel : ReactiveObject
    {
        [Reactive] public string Identifier { get; set; }
        [Reactive] public bool IsUpload { get; set; }
        [Reactive] public string Status { get; set; }
        [Reactive] public string Uri { get; set; }
        [Reactive] public string PercentCompleteText { get; set; }
        [Reactive] public double PercentComplete { get; set; }
        [Reactive] public string TransferSpeed { get; set; }
        [Reactive] public string EstimateTimeRemaining { get; set; }

        public ICommand Cancel { get; set; }
    }
}
