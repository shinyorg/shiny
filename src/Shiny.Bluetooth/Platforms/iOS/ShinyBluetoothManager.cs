using System;
using System.Reactive.Linq;
using ExternalAccessory;

//https://stackoverflow.com/questions/18884195/externalaccessory-on-ios-at-xamarin
namespace Shiny.Bluetooth
{
    public class ShinyBluetoothManager : IBluetoothManager, IBluetoothDeviceSelector
    {
        public AccessState Status => throw new NotImplementedException();


        public IObservable<IBluetoothDevice> GetConnectedDevices()
        {
            throw new NotImplementedException();
        }

        public IObservable<AccessState> RequestAccess()
        {
            throw new NotImplementedException();
        }

        public IObservable<IBluetoothDevice> Select() => Observable.Create<IBluetoothDevice>(async ob =>
        {
            var ea = EAAccessoryManager.SharedAccessoryManager;
            //ea.ConnectedAccessories
            //ea.RegisterForLocalNotifications()
            await ea.ShowBluetoothAccessoryPickerAsync(null);
            //EAAccessoryManager.Notifications.ObserveDidConnect(ea =>
            //{

            //});


            //EAAccessoryManager.SharedAccessoryManager.RegisterForLocalNotifications();

            return () => { };
        });
    }
}
//void EADidConnect(NSNotification notification)
//{
//    EAAccessory connectedAccessory = (EAAccessory)notification.UserInfo.ObjectForKey((NSString)"EAAccessoryKey");
//    Console.WriteLine("I did connect!!");
//    _accessoryList = EAAccessoryManager.SharedAccessoryManager.ConnectedAccessories;

//    // Reconnect and open the session in case the device was disconnected
//    foreach (EAAccessory acc in _accessoryList)
//    {
//        if (acc.ProtocolStrings.Contains(myDeviceProtocol))
//        {
//            // Connected to the correct accessory
//            _selectedAccessory = acc;
//            Console.WriteLine(_selectedAccessory.ProtocolStrings);

//            _EASessionController.SetupController(acc, myDeviceProtocol);
//            _EASessionController.OpenSession();

//        }
//        else
//        {
//            // Not connected
//        }
//    }

//    Console.WriteLine(connectedAccessory.Name);

//    // Update a label to show it's connected
//    lblEAConnectionStatus.Text = connectedAccessory.Name;

//}

//void EADidDisconnect(NSNotification notification)
//{

//    Console.WriteLine("Accessory disconnected");
//    _EASessionController.CloseSession();
//    lblEAConnectionStatus.Text = string.Empty;

//}


///// <summary>
///// Data receieved from accessory
///// </summary>
///// <param name="notification"></param>
//void SessionDataReceived(NSNotification notification)
//{

//    EASessionController sessionController = (EASessionController)notification.Object;

//    nuint bytesAvailable = 0;


//    while ((bytesAvailable = sessionController.ReadBytesAvailable()) > 0)
//    {

//        // read the data as a string

//        NSData data = sessionController.ReadData(bytesAvailable);
//        NSString chipNumber = new NSString(data, NSStringEncoding.UTF8);

//        // Displaying the data
//        txtMircochipNumber.Text = chipNumber;
//    }


//}


//public class EASessionController : NSStreamDelegate
//{

//    NSString SessionDataReceivedNotification = (NSString)"SessionDataReceivedNotification";

//    public static EAAccessory _accessory;
//    public static string _protocolString;

//    EASession _session;
//    NSMutableData _readData;

//    public static EASessionController SharedController()
//    {
//        EASessionController sessionController = null;

//        if (sessionController == null)
//        {
//            sessionController = new EASessionController();
//        }

//        return sessionController;

//    }

//    public void SetupController(EAAccessory accessory, string protocolString)
//    {

//        _accessory = accessory;
//        _protocolString = protocolString;

//    }

//    public bool OpenSession()
//    {

//        Console.WriteLine("opening new session");

//        _accessory.WeakDelegate = this;

//        if (_session == null)
//            _session = new EASession(_accessory, _protocolString);

//        // Open both input and output streams even if the device only makes use of one of them

//        _session.InputStream.Delegate = this;
//        _session.InputStream.Schedule(NSRunLoop.Current, NSRunLoopMode.Default);
//        _session.InputStream.Open();

//        _session.OutputStream.Delegate = this;
//        _session.OutputStream.Schedule(NSRunLoop.Current, NSRunLoopMode.Default);
//        _session.OutputStream.Open();

//        return (_session != null);

//    }

//    public void CloseSession()
//    {
//        _session.InputStream.Unschedule(NSRunLoop.Current, NSRunLoopMode.Default);
//        _session.InputStream.Delegate = null;
//        _session.InputStream.Close();

//        _session.OutputStream.Unschedule(NSRunLoop.Current, NSRunLoopMode.Default);
//        _session.OutputStream.Delegate = null;
//        _session.OutputStream.Close();

//        _session = null;

//    }


//    /// <summary>
//    /// Get Number of bytes to read into local buffer
//    /// </summary>
//    /// <returns></returns>
//    public nuint ReadBytesAvailable()
//    {
//        return _readData.Length;
//    }



//    /// <summary>
//    /// High level read method
//    /// </summary>
//    /// <param name="bytesToRead"></param>
//    /// <returns></returns>
//    public NSData ReadData(nuint bytesToRead)
//    {

//        NSData data = null;

//        if (_readData.Length >= bytesToRead)
//        {
//            NSRange range = new NSRange(0, (nint)bytesToRead);
//            data = _readData.Subdata(range);
//            _readData.ReplaceBytes(range, IntPtr.Zero, 0);
//        }

//        return data;

//    }

//    /// <summary>
//    /// Low level read method - read data while there is data and space in input buffer, then post notification to observer
//    /// </summary>
//    void ReadData()
//    {

//        nuint bufferSize = 128;
//        byte[] buffer = new byte[bufferSize];

//        while (_session.InputStream.HasBytesAvailable())
//        {
//            nint bytesRead = _session.InputStream.Read(buffer, bufferSize);

//            if (_readData == null)
//            {
//                _readData = new NSMutableData();
//            }
//            _readData.AppendBytes(buffer, 0, bytesRead);
//            Console.WriteLine(buffer);


//        }
