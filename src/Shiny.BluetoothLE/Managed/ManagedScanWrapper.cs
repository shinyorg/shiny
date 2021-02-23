using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Managed
{
    public abstract class ManagedScanWrapper<T> : IDisposable
    {
        readonly IDisposable disposable;


        protected ManagedScanWrapper(ManagedScan scan)
        {
            this.ManagedScan = scan;
            this.disposable = this.ManagedScan
                .WhenScan()
                .Subscribe(x =>
                {
                    switch (x.Action)
                    {
                        case ManagedScanListAction.Clear:
                            this.Peripherals.Clear();
                            break;

                        case ManagedScanListAction.Add:
                            var add = this.Create(x.ScanResult);
                            this.Peripherals.Add(add);
                            break;

                        case ManagedScanListAction.Update:
                            var update = this.Get<T>(x.ScanResult.Peripheral);
                            if (update != null)
                                this.Update(update, x.ScanResult);
                            break;

                        case ManagedScanListAction.Remove:
                            var remove = this.Get<T>(x.ScanResult.Peripheral);
                            if (remove != null)
                                this.Peripherals.Remove(remove);
                            break;

                    }
                });
        }


        public ManagedScan ManagedScan { get; }
        public Task Start() => this.ManagedScan.Start();
        public void Stop() => this.ManagedScan.Stop();

        public ObservableCollection<T> Peripherals { get; } = new ObservableCollection<T>();

        protected abstract T Create(ManagedScanResult result);
        protected abstract T Get<T>(IPeripheral peripheral);
        protected abstract void Update(T item, ManagedScanResult result);


        public virtual void Dispose()
        {
            this.disposable.Dispose();
            this.ManagedScan.Dispose();
        }
    }
}
