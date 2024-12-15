using Shiny.Health;

namespace Sample;


public class ListViewModel : ViewModel
{
    DataType type;

	public ListViewModel(BaseServices services, IHealthService health) : base(services)
	{
        this.DateStart = DateTime.Now.AddDays(-1);
        this.DateEnd = DateTime.Now;

        this.Load = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var start = this.DateStart.Date.Add(this.TimeStart);
                var end = this.DateEnd.Date.Add(this.TimeEnd);

                switch (this.type)
                {
                    case DataType.StepCount:
                        this.Data = (await health.GetStepCounts(start, end, Interval.Hours))
                            .Cast<object>()
                            .ToList();
                        break;

                    case DataType.HeartRate:
                        this.Data = (await health.GetAverageHeartRate(start, end, Interval.Hours))
                            .Cast<object>()
                            .ToList();
                        break;

                    case DataType.Calories:
                        this.Data = (await health.GetCalories(start, end, Interval.Hours))
                            .Cast<object>()
                            .ToList();
                        break;

                    case DataType.Distance:
                        this.Data = (await health.GetDistances(start, end, Interval.Hours))
                            .Cast<object>()
                            .ToList();
                        break;
                }
            }
        );
	}


    public ICommand Load { get; }

    [Reactive] public DateTime DateStart { get; set; }
    [Reactive] public TimeSpan TimeStart { get; set; }
    [Reactive] public DateTime DateEnd { get; set; }
    [Reactive] public TimeSpan TimeEnd { get; set; }
    [Reactive] public IList<object> Data { get; private set; }

    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        this.type = parameters.GetValue<DataType>("Type");
        switch (this.type)
        {
            case DataType.Calories:
                this.Title = "Total Calories";
                break;

            case DataType.Distance:
                this.Title = "Total Distance";
                break;

            case DataType.HeartRate:
                this.Title = "Average Heart Rate";
                break;

            case DataType.StepCount:
                this.Title = "Total Steps";
                break;
        }
        this.Load.Execute(null);
    }
}