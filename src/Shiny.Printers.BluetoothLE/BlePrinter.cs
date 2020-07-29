using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;

using Shiny.BluetoothLE;


namespace Shiny.Printers.BluetoothLE
{
    public class BlePrinter : IPrinter
    {
        readonly IPeripheral peripheral;
        readonly BlePrinterConfig config;


        public BlePrinter(IPeripheral peripheral, BlePrinterConfig config)
        {
            this.peripheral = peripheral;
            this.config = config;
        }


        public string Name => this.peripheral.Name;
        public IObservable<Unit> Connect() => this.peripheral.ConnectWait().Select(_ => Unit.Default);
        public void Disconnect() => this.peripheral.CancelConnection();


        public IObservable<Unit> Print(string content) => Observable.Create<Unit>(ob =>
            this.peripheral
                .WhenKnownCharacteristicsDiscovered(
                    this.config.ServiceUuid,
                    this.config.WriteCharacteristicUuid
                )
                .Take(1)
                .Subscribe(characteristic =>
                {
                    var ms = new MemoryStream();
                    var data = Encoding.UTF8.GetBytes(content);
                    ms.Write(data, 0, data.Length);

                    characteristic
                        .BlobWrite(ms)
                        .Subscribe(
                            _ => { },
                            ob.OnError,
                            () => ob.Respond(Unit.Default)
                        );
                })
        );
    }
}
