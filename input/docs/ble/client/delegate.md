Title: Delegate
Order: 3
---

## Delegate: Shiny.BluetoothLE.IBleDelegate 

### Task OnAdapterStateChanged(AccessState state) 
This method is used for monitoring the state of the bluetooth adapter.  This is especially important if your user turns off bluetooth which effects the application from doing what it needs to do.  For instance, say you have a smart lock that auto-unlocks when you come into range of it via Bluetooth.  If the adapter is off, this key functionality in your app is not going to function.  Users aren't always aware of this, so this event gives you a chance to hit them with something like a local notification.

### Task OnConnected(IPeripheral peripheral) 
A peripheral is often being used in the background of your application, not always a specific screen.  Processing events here is ideal because it works the same if you were in the foreground or the background of your application.

### Task OnScanResult(ScanResult result)
This event is used for listening to specific devices while the app is in the background.
