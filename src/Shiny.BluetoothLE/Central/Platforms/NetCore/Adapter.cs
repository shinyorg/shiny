using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DBus;
using Mono.BlueZ.DBus;
using org.freedesktop.DBus;


namespace Plugin.BluetoothLE.Linux
{
    public class Adapter : AbstractAdapter
    {
        readonly Adapter1 native;
        readonly GattManager1 gattManager;

        readonly ObjectManager objectManager;
        readonly AgentManager1 agentManager;
        readonly ObjectPath path;
        readonly Subject<bool> scanStatusSubj;


        //public Adapter(ObjectManager objectManager, AgentManager1 agentManger, ObjectPath path)
        //{
        //    this.objectManager = objectManager;
        //    this.native = Bus.System.GetObject<Adapter1>(BlueZPath.Service, path);
        //    this.gattManager = Bus.System.GetObject<GattManager1>(BlueZPath.Service, path);

        //    //this.gattManager.RegisterProfile();
        //    //agentManager.RequestDefaultAgent(Constants.AgentPath);
        //    //var agent = new DemoAgent ();
        //    //GattManager1 gattManager=null;
        //    //register our agent and make it the default
        //    //_system.Register (agentPath, agent);
        //    //agentManager.RequestDefaultAgent (agentPath);


        //    //var gattProfile = new BlendGattProfile();
        //    //_system.Register(gattProfilePath,gattProfile);
        //    //gattManager.RegisterProfile(gattProfilePath,new string[]{charRead},new Dictionary<string,object>());
        //    //System.Console.WriteLine("Registered gatt profile");

        //    this.agentManager = agentManger;
        //    this.path = path;
        //    this.scanStatusSubj = new Subject<bool>();
        //}


        public override bool IsScanning => this.native.Discovering;
        public override IObservable<bool> WhenScanningStatusChanged() => this.scanStatusSubj;


        public override IObservable<IScanResult> Scan(ScanConfig config = null) => Observable.Create<IScanResult>(ob =>
        {

            return () =>
            {

            };
        });


        public override IObservable<IScanResult> ScanListen() => Observable.Create<IScanResult>(ob =>
        {
            this.native.SetDiscoveryFilter(new Dictionary<string, object> {{"Transport", "le" }});
            this.native.StartDiscovery();
            this.scanStatusSubj.OnNext(true);
            // TODO: clear peripheral manager

            var dbusName = typeof(Device1).DBusInterfaceName();

            var sub = Observable
                .Interval(TimeSpan.FromMilliseconds(500))
                .Subscribe(_ =>
                {
                    var managedObjects = this.objectManager.GetManagedObjects();
                    foreach (var key in managedObjects.Keys)
                    {
                        if (key.ToString().StartsWith(this.path.ToString()))
                        {
                            var obj = managedObjects[key][dbusName]; // TODO: careful
                            if (obj != null)
                            {
                                var dev = Bus.System.GetObject<Device1>(BlueZPath.Service, key);
                                // TODO: add peripheral
                            }
                        }
                    }
                });

            return () =>
            {
                this.scanStatusSubj.OnNext(false);
                this.native.StopDiscovery();
                sub.Dispose();
            };
        })
        .Publish()
        .RefCount();


        public override IObservable<AdapterStatus> WhenStatusChanged() => Observable.Return(AdapterStatus.PoweredOn);
    }
}
