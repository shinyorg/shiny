using System.Linq;
using Shiny.Health;

namespace Sample;


public class HealthTestViewModel : ViewModel
{
    public HealthTestViewModel(
        BaseServices services,
        IHealthService health
    ) : base(services)
    {
        this.Start = DateTime.Now.AddDays(-1).Date;
        this.End = this.Start.ToEndOfDay();

        this.Load = ReactiveCommand.CreateFromTask(async () =>
        {
            var result = await health.RequestPermissions(
                DataType.Calories,
                DataType.Distance,
                DataType.HeartRate,
                DataType.StepCount
            );
            //if (!result)
            //{
            //    await this.Dialogs.Alert("Failed permission check");
            //    return;
            //}

            if (this.Start < this.End)
            {
                this.ErrorText = String.Empty;
            }
            else
            {
                this.ErrorText = "Start date must be greater than End date";
                this.Calories = 0;
                this.HeartRate = 0;
                this.Distance = 0;
                this.Steps = 0;
                return;
            }

            this.Distance = (await health.GetDistances(this.Start, this.End, Interval.Days)).Sum(x => x.Value);
            this.Calories = (await health.GetCalories(this.Start, this.End, Interval.Days)).Sum(x => x.Value);            
            this.Steps = (await health.GetStepCounts(this.Start, this.End, Interval.Days)).Sum(x => x.Value);
            this.HeartRate = (await health.GetAverageHeartRate(this.Start, this.End, Interval.Days)).Average(x => x.Value);
        });
        this.BindBusyCommand(this.Load);

        this.NavToList = ReactiveCommand.CreateFromTask(async (string arg) =>
        {
            var type = Enum.Parse<DataType>(arg);
            await this.Navigation.Navigate("ListPage", ("Type", type));
        });
    }


    public override void OnAppearing()
    {
        base.OnAppearing();
        this.Load.Execute(null);
    }

    public ICommand Load { get; }
    public ICommand NavToList { get; }
    [Reactive] public DateTimeOffset Start { get; set; }
    [Reactive] public DateTimeOffset End { get; set; }

    [Reactive] public string? ErrorText { get; private set; }
    [Reactive] public double Steps { get; private set; }
    [Reactive] public double Calories { get; private set; }
    [Reactive] public double Distance { get; private set; }
    [Reactive] public double HeartRate { get; private set; }
}