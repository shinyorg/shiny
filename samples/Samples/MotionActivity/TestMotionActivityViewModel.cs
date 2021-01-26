using System;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny.Locations;
using Shiny.Testing.Locations;


namespace Samples.MotionActivity
{
    public class TestMotionActivityViewModel : ViewModel
    {
        public TestMotionActivityViewModel(IMotionActivityManager manager, IDialogs dialogs)
        {
            var tm = manager as TestMotionActivityManager;

            this.IsGeneratingData = tm?.IsGeneratingTestData ?? false;
            this.ActivityType = tm?.GeneratingActivityType ?? MotionActivityType.Automotive;
            this.Confidence = tm?.GeneratingConfidence ?? MotionActivityConfidence.High;
            this.IntervalSeconds = Convert.ToInt32(tm?.GeneratingInterval?.TotalSeconds ?? 10d);

            this.SelectActivityType = dialogs.PickEnumValueCommand<MotionActivityType>(
                "Select Activity Type",
                x => this.ActivityType = x
            );
            this.SelectConfidence = dialogs.PickEnumValueCommand<MotionActivityConfidence>(
                "Select Confidence",
                x => this.Confidence = x
            );

            this.Toggle = ReactiveCommand.Create(
                () =>
                {
                    if (tm.IsGeneratingTestData)
                    {
                        tm.StopGeneratingTestData();
                        this.IsGeneratingData = false;
                    }
                    else
                    {
                        tm.StartGeneratingTestData(
                            this.ActivityType,
                            TimeSpan.FromSeconds(this.IntervalSeconds),
                            this.Confidence
                        );
                        this.IsGeneratingData = true;
                    }
                },
                this.WhenAny(
                    x => x.IntervalSeconds,
                    (ts) =>
                    {
                        if (tm == null)
                            return false;

                        if (ts.GetValue() <= 0)
                            return false;

                        return true;
                    }
                )
            );
        }


        public ICommand Toggle { get; }
        public ICommand SelectActivityType { get; }
        public ICommand SelectConfidence { get; }

        [Reactive] public bool IsGeneratingData { get; private set; }
        [Reactive] public MotionActivityType ActivityType { get; private set; } = MotionActivityType.Automotive;
        [Reactive] public MotionActivityConfidence Confidence { get; private set; } = MotionActivityConfidence.High;
        [Reactive] public int IntervalSeconds { get; set; } = 10;
    }
}
