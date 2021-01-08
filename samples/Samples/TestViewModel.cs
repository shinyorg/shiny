using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny.BluetoothLE;


namespace Samples
{
    public class TestViewModel : ViewModel
    {
        IPeripheral peripheral;
        CompositeDisposable disp;


        public TestViewModel(IBleManager bleManager)
        {
            this.Start = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    this.disp = new CompositeDisposable();

                    this.Logs = String.Empty;
                    this.IsBusy = true;
                    this.Append("Scanning...");

                    this.peripheral = await bleManager
                        .ScanForUniquePeripherals(new ScanConfig { ServiceUuids = { "FFF0" } })
                        .Take(1)
                        .ToTask();

                    this.Append("Device Found");
                    await this.peripheral.ConnectWait();
                    this.Append("Device Connected");
                    var rx = await this.peripheral.GetKnownCharacteristic("FFF0", "FFF1").ToTask();
                    if (rx == null)
                    {
                        this.Append("RX Not Found");
                        return;
                    }
                    this.Append("RX Found");


                    var tx = await this.peripheral.GetKnownCharacteristic("FFF0", "FFF2").ToTask();
                    if (tx == null)
                    {
                        this.Append("TX NOT Found");
                        return;
                    }
                    this.Append("TX Found");

                    this.disp.Add(rx
                        .Notify()
                        .Select(x => x.Data)
                        .SubOnMainThread(
                            data => this.Append($"[RECEIVE] {data}"),
                            ex => this.Append($"RX Error {ex}")
                        )
                    );

                    this.disp.Add(Observable
                        .Interval(TimeSpan.FromSeconds(1))
                        .Select(_ => tx.Write(new byte[] { 0x01, 0x01 }))
                        .SubOnMainThread(_ => this.Append("WRITE"))
                    );
                },
                this.WhenAny(x => x.IsBusy, x => !x.GetValue())
            );
            this.Stop = ReactiveCommand.Create(
                () =>
                {
                    this.disp?.Dispose();
                    this.peripheral?.CancelConnection();
                    this.Append("Stopped");
                    this.IsBusy = false;
                },
                this.WhenAny(x => x.IsBusy, x => x.GetValue())
            );
        }


        public ICommand Start { get; }
        public ICommand Stop { get; }
        [Reactive] public string Logs { get; private set; }

        void Append(string txt) => this.Logs = $"{txt}{Environment.NewLine}{this.Logs}";
    }
}
