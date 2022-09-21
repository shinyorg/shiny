using Shiny;
using Shiny.Beacons;

namespace Sample.Beacons;


public class CreateViewModel : ViewModel
{
    const string EstimoteUuid = "B9407F30-F5F8-466E-AFF9-25556B57FE6D";


    public CreateViewModel(BaseServices services) : base(services)
    {
        this.EstimoteDefaults = ReactiveCommand.Create(() =>
        {
            this.Identifier = "Estimote";
            this.Uuid = EstimoteUuid;
        });

        this.WhenAnyValue(x => x.Major)
            .Select(x => !x.IsEmpty() && UInt16.TryParse(x, out _))
            .ToPropertyEx(this, x => x.IsMajorSet);

        this.Use = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var region = this.GetBeaconRegion();
                await this.Navigation.GoBack(false, (nameof(BeaconRegion), region));
            },
            this.WhenAny(
                x => x.Identifier,
                x => x.Uuid,
                x => x.Major,
                x => x.Minor,
                x => x.NotifyOnEntry,
                x => x.NotifyOnExit,
                (idValue, uuidValue, majorValue, minorValue, entry, exit) => 
                {
                    if (this.ForMonitoring)
                    {
                        var atLeast1Notification = entry.GetValue() || exit.GetValue();
                        if (!atLeast1Notification)
                            return false;
                    }
                    return this.IsValid();
                }
            )
        );
    }


    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        if (parameters.ContainsKey("Monitoring"))
            this.ForMonitoring = parameters.GetValue<bool>("Monitoring");
    }

    [ObservableAsProperty] public bool IsMajorSet { get; }
    public ICommand Use { get; }
    public ICommand EstimoteDefaults { get; }

    [Reactive] public bool ForMonitoring { get; private set; }
    [Reactive] public string Identifier { get; set; }
    [Reactive] public string Uuid { get; set; }
    [Reactive] public string Major { get; set; }
    [Reactive] public string Minor { get; set; }
    [Reactive] public bool NotifyOnEntry { get; set; } = true;
    [Reactive] public bool NotifyOnExit { get; set; } = true;


    BeaconRegion GetBeaconRegion() => new BeaconRegion(
        this.Identifier,
        Guid.Parse(this.Uuid),
        GetNumberAddress(this.Major),
        GetNumberAddress(this.Minor)
    )
    {
        NotifyOnEntry = this.NotifyOnEntry,
        NotifyOnExit = this.NotifyOnExit
    };


    bool IsValid()
    {
        if (this.Identifier.IsEmpty())
            return false;

        if (!this.Uuid.IsEmpty() && !Guid.TryParse(this.Uuid, out _))
            return false;

        if (!ValidateNumberAddress(this.Major))
            return false;

        if (!ValidateNumberAddress(this.Minor))
            return false;

        return true;
    }


    static bool ValidateNumberAddress(string value)
    {
        if (value.IsEmpty())
            return true;

        return UInt16.TryParse(value, out _);
    }


    static ushort? GetNumberAddress(string value)
    {
        if (value.IsEmpty())
            return null;

        return UInt16.Parse(value);
    }
}
