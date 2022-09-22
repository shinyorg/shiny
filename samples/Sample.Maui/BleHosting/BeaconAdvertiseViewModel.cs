using Shiny.BluetoothLE.Hosting;

namespace Sample.BleHosting;


public class BeaconAdvertiseViewModel : ViewModel
{
    public BeaconAdvertiseViewModel(BaseServices services, IBleHostingManager hostingManager) : base(services)
    {
        this.IsAdvertising = hostingManager.IsAdvertising;

        this.Toggle = ReactiveCommand.CreateFromTask(
            async () =>
            {
                if (this.IsAdvertising)
                {
                    hostingManager.StopAdvertising();
                }
                else
                {
                    await hostingManager.AdvertiseBeacon(
                        Guid.Parse(this.Uuid!),
                        (ushort)this.Major,
                        (ushort)this.Minor
                    );
                }
                this.IsAdvertising = !this.IsAdvertising;
            },
            this.WhenAny(
                x => x.Uuid,
                x => x.Major,
                x => x.Minor,
                (uuid, major, minor) =>
                {
                    if (!Guid.TryParse(uuid.GetValue(), out var _))
                        return false;

                    if (!IsValid(major.GetValue()))
                        return false;

                    if (!IsValid(minor.GetValue()))
                        return false;

                    return true;
                }
            )
        );

        this.Generate = ReactiveCommand.Create(() =>
        {
            this.Uuid = Guid.NewGuid().ToString();
            var r = new Random();
            this.Major = r.Next(1, ushort.MaxValue);
            this.Minor = r.Next(1, ushort.MaxValue);
        });
    }


    public ICommand Toggle { get; }
    public ICommand Generate { get; }

    [Reactive] public bool IsAdvertising { get; private set; }
    [Reactive] public string Uuid { get; set; }
    [Reactive] public int Major { get; set; }
    [Reactive] public int Minor { get; set; }
    //[Reactive] public int TxPower { get; set; }


    static bool IsValid(int value) => value >= 1 && value <= ushort.MaxValue;
}
