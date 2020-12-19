using System;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples
{
    public class CommandItem : ReactiveObject
    {
        [Reactive] public string ImageUri { get; set; }
        [Reactive] public string Text { get; set; }
        [Reactive] public string Detail { get; set; }
        public ICommand PrimaryCommand { get; set; }
        public ICommand SecondaryCommand { get; set; }
        public object Data { get; set; }
    }
}
