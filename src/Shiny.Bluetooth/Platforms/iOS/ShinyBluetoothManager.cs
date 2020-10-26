using System;
using System.Reactive.Linq;
using ExternalAccessory;

//https://stackoverflow.com/questions/18884195/externalaccessory-on-ios-at-xamarin
namespace Shiny.Bluetooth
{
    public class ShinyBluetoothManager : IBluetoothManager, IBluetoothDeviceSelector
    {
        public IObservable<IBluetoothDevice> Select() => Observable.Create<IBluetoothDevice>(async ob =>
        {
            var ea = EAAccessoryManager.SharedAccessoryManager;
            //ea.ConnectedAccessories
            //ea.RegisterForLocalNotifications()
            await ea.ShowBluetoothAccessoryPickerAsync(null);
            //NSNotificationCenter.DefaultCenter.AddObserver(EAAccessoryManager.DidConnectNotification, EADidConnect);
            //NSNotificationCenter.DefaultCenter.AddObserver(EAAccessoryManager.DidDisconnectNotification, EADidDisconnect);
            //NSNotificationCenter.DefaultCenter.AddObserver(SessionDataReceivedNotification, SessionDataReceived);
            //EAAccessoryManager.SharedAccessoryManager.RegisterForLocalNotifications();

            return () => { };
        });
    }
}
