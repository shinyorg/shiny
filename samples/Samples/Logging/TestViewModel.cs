using System;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny.Logging;

namespace Samples.Logging
{
    public class TestViewModel : ViewModel
    {
        IDisposable errSub;
        IDisposable eventSub;


        public TestViewModel()
        {
            this.ToggleErrorStream = ReactiveCommand.Create(() =>
            {
                if (this.errSub != null)
                {
                    this.IsErrorsEnabled = false;
                    this.errSub.Dispose();
                    this.errSub = null;
                }
                else
                {
                    this.IsErrorsEnabled = true;
                    this.errSub = Observable
                        .Interval(TimeSpan.FromSeconds(10))
                        .StartWith(0)
                        .Subscribe(_ => Log.Write(
                            new Exception("TEST ERROR"),
                            ("Parameter1", "Hello"),
                            ("Parameter2", "20")
                        ));
                }
            });
            this.ToggleEventStream = ReactiveCommand.Create(() =>
            {
                if (this.eventSub != null)
                {
                    this.eventSub.Dispose();
                    this.eventSub = null;
                    this.IsEventsEnabled = false;
                }
                else
                {
                    this.IsEventsEnabled = true;
                    this.eventSub = Observable
                        .Interval(TimeSpan.FromSeconds(1))
                        .StartWith(0)
                        .Subscribe(_ => Log.Write(
                            "TEST EVENT",
                            "HELLO",
                            ("Parameter1", "Event"),
                            ("Parameter2", "99")
                        ));
                }
            });
        }


        [Reactive] public bool IsEventsEnabled { get; private set; }
        [Reactive] public bool IsErrorsEnabled { get; private set; }
        public ICommand ToggleErrorStream { get; }
        public ICommand ToggleEventStream { get; }


        public override void Destroy()
        {
            base.Destroy();
            this.errSub?.Dispose();
            this.eventSub?.Dispose();
        }
    }
}
