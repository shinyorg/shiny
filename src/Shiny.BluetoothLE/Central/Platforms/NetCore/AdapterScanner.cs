using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using Tmds.DBus;


namespace Plugin.BluetoothLE.Linux
{
    //https://github.com/brookpatten/Mono.BlueZ/blob/master/Mono.BlueZ.Console/BlendMicroBootstrap.cs
    public class AdapterScanner : IAdapterScanner
    {
        public bool IsSupported => Bus.System?.IsConnected ?? false;


        static IObservable<Unit> dbusLoop;


        public static IObservable<Unit> DBusLoop()
        {
            dbusLoop = dbusLoop ?? Observable.Create<Unit>(ob =>
            {
                var cancel = false;
                while (!cancel)
                    Bus.System.Iterate();

                return () => cancel = true;
            })
            .Publish()
            .RefCount();

            return dbusLoop;
        }


        public IObservable<IAdapter> FindAdapters() => Observable.Create<IAdapter>(ob =>
        {
            var objectManager = Bus.System.GetObject<ObjectManager>(BlueZPath.Service, ObjectPath.Root);
            var agentManager = Bus.System.GetObject<AgentManager1>(BlueZPath.Service, new ObjectPath("/org/bluez"));

            objectManager.InterfacesAdded += (path, i) =>
            {
                //ob.OnNext(new Adapter(objectManager, agentManager, path));
            };
            //manager.InterfacesRemoved += (p, i) =>

            var managedObjects = objectManager.GetManagedObjects();
            var dbusName = typeof(LEAdvertisingManager1).DBusInterfaceName();

            foreach (var path in managedObjects.Keys)
            {
                if (managedObjects[path].ContainsKey(dbusName))
                {
                    //ob.OnNext(new Adapter(objectManager, agentManager, path));
                }
            }
            return DBusLoop().Subscribe();
        });
    }
}
/*
 *
using System;

using DBus;
using org.freedesktop.DBus;

namespace Mono.BlueZ.DBus
{
	/// <summary>
	/// constants and utility functions to quickly locate bluez paths
	/// inside dbus
	/// </summary>
	public static class BlueZPath
	{
		public const string Service = "org.bluez";
		public const string RootString = "/org/bluez";
		public static readonly ObjectPath Root = new ObjectPath(RootString);

		public static string AdapterString (string name)
		{
			return string.Format ("{0}/{1}", RootString, name);
		}

		public static ObjectPath Adapter (string name)
		{
			if (string.IsNullOrWhiteSpace (name))
			{
				throw new ArgumentNullException ("name");
			}
			name = name.ToLower ();
			if (!name.StartsWith ("hci"))
			{
				throw new ArgumentException ("Adapter name must start with hci");
			}
			return new ObjectPath (AdapterString(name));
		}

		/// <summary>
		/// Takes a peripheral addess of format XX:XX:XX:XX:XX:XX
		/// returns component of peripheral path eg dev_XX_XX_XX_XX_XX_XX
		/// </summary>
		/// <returns>The component.</returns>
		/// <param name="deviceAddress">Peripheral address.</param>
		public static string DeviceComponent (string deviceAddress)
		{
			//there's obviously more we could do to validate this, but we're just going to
			//unwisely assume some level of competance on the caller
			if (string.IsNullOrWhiteSpace (deviceAddress)
			    || deviceAddress.Length != 17
			    || deviceAddress[2]!=':'
			    || deviceAddress[5]!=':'
			    || deviceAddress[8]!=':'
			    || deviceAddress[11]!=':'
			    || deviceAddress[14]!=':')
			{
				throw new FormatException ("peripheral address is of an invalid format");
			}
			return string.Format("dev_{0}",deviceAddress.ToUpper ().Replace (":", "_"));
		}

		public static string DeviceString (string adapterName, string deviceAddress)
		{
			return string.Format ("{0}/{1}", AdapterString (adapterName), DeviceComponent (deviceAddress));
		}

		public static ObjectPath Peripheral (string adapterName, string deviceAddress)
		{
			return new ObjectPath (DeviceString (adapterName, deviceAddress));
		}

		public static string GattServiceString (string adapter, string deviceAddress, string serviceId)
		{
			return string.Format ("{0}/service{1}", DeviceString (adapter, deviceAddress),serviceId);
		}

		public static ObjectPath GattService (string adapter, string deviceAddress, string serviceId, string charId)
		{
			return new ObjectPath (GattServiceString (adapter, deviceAddress, serviceId));
		}

		public static string GattCharacteristicString (string adapter, string deviceAddress, string serviceId, string charId)
		{
			return string.Format ("{0}/char{1}", GattServiceString (adapter, deviceAddress,serviceId),charId);
		}

		public static ObjectPath GattCharacteristic (string adapter, string deviceAddress, string serviceId, string charId)
		{
			return new ObjectPath (GattCharacteristicString (adapter, deviceAddress, serviceId, charId));
		}
	}
} *
 *
 * using System;
using System.Threading;

using DBus;

namespace Mono.BlueZ.DBus
{
	public class DBusConnection:IDisposable
	{
		private Bus _system;
		public Exception _startupException{ get; private set; }
		private ManualResetEvent _started = new ManualResetEvent(false);
		private Thread _dbusLoop;
		private bool _run=false;
		private bool _isStarted=false;

		public DBusConnection ()
		{
			Startup ();
		}

		public Bus System
		{
			get
			{
				if (_isStarted)
				{
					return _system;
				}
				else
				{
					throw new InvalidOperationException ("Not connected to DBus");
				}
			}
		}

		private void Startup()
		{
			// Run a message loop for DBus on a new thread.
			_run = true;
			_dbusLoop = new Thread(DBusLoop);
			_dbusLoop.IsBackground = true;
			_dbusLoop.Start();
			_started.WaitOne(60 * 1000);
			_started.Close();
			if (_startupException != null)
			{
				throw _startupException;
			}
			else
			{
				_isStarted = true;
			}
		}

		private void DBusLoop()
		{
			try
			{
				_system=Bus.System;
			}
			catch (Exception ex)
			{
				_startupException = ex;
				return;
			}
			finally
			{
				_started.Set();
			}

			while (_run)
			{
				_system.Iterate();
			}
		}

		private void Shutdown()
		{
			_run = false;
			try
			{
				_dbusLoop.Join(1000);
			}
			catch
			{
				try
				{
					_dbusLoop.Abort ();
				}
				catch
				{
				}
			}
		}

		public void Dispose()
		{
			if (_isStarted) {
				Shutdown ();
			}
		}
	}
}
	string serviceUUID="713d0000-503e-4c75-ba94-3148f18d941e";
	string charVendorName = "713D0001-503E-4C75-BA94-3148F18D941E";
	string charRead = "713D0002-503E-4C75-BA94-3148F18D941E";//rx
	string charWrite = "713D0003-503E-4C75-BA94-3148F18D941E";//tx
	string charAck = "713D0004-503E-4C75-BA94-3148F18D941E";
	string charVersion = "713D0005-503E-4C75-BA94-3148F18D941E";
	string clientCharacteristic = "00002902-0000-1000-8000-00805f9b34fb";


		//get a dbus proxy to the adapter
		var adapter = GetObject<Adapter1> (Service, adapterPath);
		gattManager = GetObject<GattManager1>(Service,adapterPath);
		var gattProfile = new BlendGattProfile();
		_system.Register(gattProfilePath,gattProfile);
		gattManager.RegisterProfile(gattProfilePath,new string[]{charRead},new Dictionary<string,object>());
		System.Console.WriteLine("Registered gatt profile");

		//assume discovery for ble
		//scan for any new peripherals
		System.Console.WriteLine("Starting LE Discovery...");
		var discoveryProperties = new Dictionary<string,object>();
		discoveryProperties["Transport"]="le";
		adapter.SetDiscoveryFilter(discoveryProperties);
		adapter.StartDiscovery ();
		Thread.Sleep(5000);//totally arbitrary constant, the best kind
		//Thread.Sleep ((int)adapter.DiscoverableTimeout * 1000);

		//refresh the object graph to get any peripherals that were discovered
		//arguably we should do this in the objectmanager added/removed events and skip the full
		//refresh, but I'm lazy.
		System.Console.WriteLine("Discovery complete, refreshing");
		managedObjects = manager.GetManagedObjects();

		foreach (var obj in managedObjects.Keys) {
			if (obj.ToString ().StartsWith (adapterPath.ToString ())) {
				if (managedObjects [obj].ContainsKey (typeof(Device1).DBusInterfaceName ())) {

					var managedObject = managedObjects [obj];
					if(managedObject[typeof(Device1).DBusInterfaceName()].ContainsKey("Name"))
					{
						var name = (string)managedObject[typeof(Device1).DBusInterfaceName()]["Name"];

						if (name.StartsWith ("MrGibbs"))
						{
							System.Console.WriteLine ("Peripheral " + name + " at " + obj);
							var peripheral = _system.GetObject<Device1> (Service, obj);

							var uuids = peripheral.UUIDs;
							foreach(var uuid in peripheral.UUIDs)
							{
								System.Console.WriteLine("\tUUID: "+uuid);
							}

							peripherals.Add(peripheral);

						}
					}
				}
			}
		}

		var readCharPath = new ObjectPath("/org/bluez/hci0/dev_F6_58_7F_09_5D_E6/service000c/char000f");
		var  readChar= GetObject<GattCharacteristic1>(Service,readCharPath);
		var properties = GetObject<Properties>(Service,readCharPath);

		properties.PropertiesChanged += new PropertiesChangedHandler(
			new Action<string,IDictionary<string,object>,string[]>((@interface,changed,invalidated)=>{
				System.Console.WriteLine("Properties Changed on "+@interface);
				if(changed!=null)
				{
					foreach(var prop in changed.Keys)
					{
						if(changed[prop] is byte[])
						{
							foreach(var b in ((byte[])changed[prop]))
							{
								System.Console.Write(b+",");
							}
							System.Console.WriteLine("");
						}
						else
						{
							System.Console.WriteLine("{0}={1}",prop,changed[prop]);
						}
					}
				}

				if(invalidated!=null)
				{
					foreach(var prop in invalidated)
					{
						System.Console.WriteLine(prop+" Invalidated");
					}
				}
			}));

		foreach(var peripheral in peripherals)
		{
			System.Console.WriteLine("Connecting to "+peripheral.Name);
			peripheral.Connect();
			System.Console.WriteLine("\tConnected");
		}

		readChar.StartNotify();

		System.Threading.Thread.Sleep(10000);

		readChar.StopNotify();
		System.Threading.Thread.Sleep(500);
	}
	finally
	{
		if (peripherals != null) {
			foreach(var peripheral in peripherals)
			{
				System.Console.WriteLine("Disconnecting "+peripheral.Name);
				peripheral.Disconnect();
				System.Console.WriteLine("\tDisconnected");
			}
		}
		agentManager.UnregisterAgent (agentPath);
		gattManager.UnregisterProfile (gattProfilePath);
	}
}


public static byte[] CombineArrays( params byte[][] array )
{
	var rv = new byte[array.Select( x => x.Length ).Sum()];

	for ( int i = 0, insertionPoint = 0; i < array.Length; insertionPoint += array[i].Length, i++ )
		Array.Copy( array[i], 0, rv, insertionPoint, array[i].Length );
	return rv;
}


public class BlendGattProfile:GattProfile1
{
public BlendGattProfile()
{
}

public void Release()
{
	System.Console.WriteLine ("GattProfile1.Release");
}

}
*/