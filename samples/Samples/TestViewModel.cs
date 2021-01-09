using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
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
                            data => this.Append($"[RECEIVE] {Encoding.ASCII.GetString(data)}"),
                            ex => this.Append($"RX Error {ex}")
                        )
                    );

                    //this.Append("Initializing");
                    //await tx.Write(Encoding.ASCII.GetBytes("ATZ")).ToTask();
                    //await tx.Write(Encoding.ASCII.GetBytes("AT RV")).ToTask();
                    //await tx.Write(Encoding.ASCII.GetBytes("ATI")).ToTask();
                    //await tx.Write(Encoding.ASCII.GetBytes("AT PC")).ToTask();
                    //await tx.Write(Encoding.ASCII.GetBytes("AT D")).ToTask();
                    //await tx.Write(Encoding.ASCII.GetBytes("AT E0")).ToTask();
                    //await tx.Write(Encoding.ASCII.GetBytes("AT SP 5")).ToTask();
                    //await tx.Write(Encoding.ASCII.GetBytes("AT ST 80")).ToTask();
                    //await tx.Write(Encoding.ASCII.GetBytes("AT SH 81 10 F0")).ToTask();
                    //await tx.Write(Encoding.ASCII.GetBytes("21 0B 01")).ToTask();
                    //this.Append("Initialized");

                    var bytes = Encoding.ASCII.GetBytes("010D\r");

                    this.disp.Add(Observable
                        .Interval(TimeSpan.FromSeconds(3))
                        .Select(_ => tx.Write(bytes))
                        .Switch()
                        .SubOnMainThread(
                            x => this.Append("WRITE"),
                            ex => this.Append("WRITE ERROR")
                        )
                        //.Subscribe(async _ =>
                        //{
                        //    try
                        //    {
                        //        await tx.Write(bytes).ToTask();
                        //        this.Append("WRITE");
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        this.Append("ERROR: " + ex);
                        //    }
                        //})
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
