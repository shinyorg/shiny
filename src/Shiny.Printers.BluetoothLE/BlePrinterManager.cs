using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Shiny.BluetoothLE;


namespace Shiny.Printers.BluetoothLE
{
    public class BlePrinterManager : IPrinterManager
    {
        readonly IBleManager bleManager;
        readonly BlePrinterConfig config;


        public BlePrinterManager(IBleManager bleManager, BlePrinterConfig config)
        {
            this.bleManager = bleManager;
            this.config = config;
        }


        public Task<AccessState> RequestAccess() => this.bleManager.RequestAccess().ToTask();


        public IObservable<IPrinter> Scan()
        {
            var cfg = new ScanConfig
            {
                ServiceUuids = new List<string>
                {
                    this.config.ScanServiceUuid
                }
            };
            return this.bleManager
                .Scan(cfg)
                .Select(x => new BlePrinter(x.Peripheral, this.config));
        }
    }
}
