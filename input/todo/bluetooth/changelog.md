# CHANGE LOG

## 6.2.3
* [fix][ios] CBPeripheralManager restoration is now enabled by default and requires no user setting
* [fix][android] Characteristic equality safety checks

## 6.2.2
* [feature][ios] Get connected devices supports service UUID filter which allows iOS to retrieve connected device list outside of application
* [fix][macos] Fix valuetuple issue that was preventing compile
* [breaking] BlobWrite has been moved to extension method in preparation for updates in 6.3

## 6.2.1
* [fix][android] Fix threading issue on android characteristics
* [fix][ios] Advertiser NRE

## 6.2.0
* [fix][android] DiscoverServices issues a refresh call to internal methods
* [fix][ios] DiscoverServices now completes and bubbles through errors properly
* [fix][ios] Creating a GATT server now waits 
* [fix][uwp] Device caching is now cleared when device connection is cancelled
* [fix][uwp] StopScan implemented
* [feature] Android settings now exist in .netstandard level to allow for centralized config
* [feature] WhenReadyStartServer is now available to monitor the overall state of the bluetooth adapter before starting a gatt server
* [breaking] CreateGattServer is now a completable observable so that iOS can properly detect state of adapter before starting
* [breaking] Min dependency on RX v4 now

## 6.1.3
* [fix][ios] Adapter.WhenStatusChanged() could return wrong status under certain conditions
* [fix][android] Fixes & improvements around connection errors
* [fix] ConnectHook now bubbles through errors properly
* [fix] Ensure other connection extension methods are respecting connection errors

## 6.1.2
* [fix][ios] RSSI not returning
* Update to newest ACR core to allow for RXv4

## 6.1.1
* [fix][uwp] MAC address parsing

## 6.1.0
* [breaking] GetKnownDevice, GetPairDevices, & GetConnectedDevices all return observables now
* [minor] Allow new System.Reactive v4 package
* [fix] buffer mismatch check in BlobWrite unnecessary now
* [fix] blobwrite now cleans tail of buffer
* [fix] device extensions for Write/ReadCharacteristic no longer auto-connect
* [fix] ConnectHook now reconnects and rehooks characteristic
* [fix][droid] read rssi now returns properly on all devices
* [fix][droid] device manager queue cleans up 10x faster
* [fix][droid] more cancellation fixes to droid queue
* [fix][droid] advertising startup checks fixed for android v4.x
* [feature] Return of Adapter.ScanInterval
* [feature][droid] Scan batching

## 6.0.3
* [fix] reconnect read/write race condition
* [feature] Return of device based RSSI requests (Device.ReadRssi() & Device.ReadRssiContinuously())
* [feature][droid] batch scanning - courtesy of goiditdev 

## 6.0.2
* [fix] ConnectWait should cancel the connection if subscription is disposed before connection is successful
* [fix] ConnectWait listens to connection failures for cancellation
* [fix] Additional NRE fixes on cancelling connection
* [fix][droid] Characteristic should not throw error, should call OnError
* [fix][droid] WhenKnownCharacteristicDiscovered was not firing properly

## 6.0.1
* [fix] RegisterAndNotify cleanup would fail if disposed after connection has been severed

## 6.0
* [feature][breaking] GATT server & BLE advertising are now separate functions
* [feature] ScanConfig is now available on all Adapter extension methods
* [feature] WhenKnownCharacteristicsDiscovered is a handy way of always calling for known characteristics as device connects
* [feature] ConnectHook is a new extension that manages everything from the connection/reconnect/disconnect to the characteristic notification
* [feature] Device.WriteCharacteristic and Device.ReadCharacteristic are new extensions making it easy to read/write to characteristics without a reference to one
* [feature] StopScan for cancelling background scans where the observable may have been lost
* [feature] WriteWithoutResponse is now async (Android - so it can participate in queue & iOS since it provides a ready event)
* [breaking] GATT server now starts as soon as it is created (removal of start/stop functions)
* [breaking][feature] All read/write/notification actions now contain a result object (good or bad) instead of calling OnError - why?  because no one knows how to use RX so I'll do the fun stuff for them!
* [breaking] Write no longer fallsback to "WriteWithoutResponse"
* [breaking] Connect is no longer observable - it is designed to be async for when device comes into range
* [breaking] Many methods (Device rssi, Adapter Scan events) have been removed to simplify API as well as remove potential issues due to their use
* [fix][android] fixes to locking mechanism as well as ability to disable it via CrossBleAdapter.AndroidDisableLockMechanism

## 5.3.1
* [fix][android] GetKnownCharacteristic now sync locks to prevent android race condition
* [feature][android] configurable parameter to allow "breather" time between operations to prevent GATT issues

## 5.3
* [feature][android] Advertisement service UUID filtering for Pre-Lollipop
* [fix][android] Fix issue with stopping scan when bluetooth adapter becomes disabled
* [fix][android] Fix NRE with reconnection WhenServiceDiscovered
* [fix][android] More improvements to race conditions
* [BREAKING] Adapter.ScanListen has been removed

## 5.2.2
* [feature] push .NET standard 2.0
* [feature] push to android 8 (forced nuget compile target - Android 4.3+ is still supported)
* [fix][android] fix race conditions around semaphore cleanup

## 5.2
* [fix][android] more connection fixes to alleviate GATT 133
* [fix][android] advertisement service UUIDs not parsing properly

## 5.1
* [fix][android] rewritten connect/disconnect logic
* [fix][android][ios] rewritten reconnection logic
* [fix][android] kill more gatt133 errors by forcing synchronized communication (hidden to consumer)
* [feature][android] Ability to use Android AutoConnect option on connect
* [feature] scan for multiple UUIDs

## 5.0
* [breaking][feature] SetNotificationValue has been replaced with EnableNotifications/DisableNotifications.
* [fix][ios] NRE when read/notification value is null
* [fix][android] service UUIDs in advertisement not being parsed correctly
* [fix][android] cleanup internal thread delegation
* [fix][uwp] don't marshall to main thread for most calls
* [fix][uwp] mac address length

## 4.0.1
* [fix][ios] NRE race condition in GetKnownCharacteristic

## 4.0
* [feature] .net standard support
* [feature][breaking] characteristics must now have their notifications enabled/disabled using characteristic.SetNotificationValue(..);
* [uwp] Connection status and general connection keep-alive improvements

## 3.1
* [feature] UWP client beta (server support has not been tested)
* [feature] Adapter scanner now has an empty implementation for platforms where it is not supported


## 3.0
* [feature] GATT Server is now built into this library
* [feature] Manufacturer data can be advertised on Windows and Android
* [feature] ability to scan for multiple bluetooth adapters
* [feature] expose service data as part of advertisement data
* [feature] expose native device from IDevice as object
* [feature] New methods - Device.GetKnownService, Service.GetKnownCharacteristics(uuids), and Device.GetKnownCharacteristics(serviceUuid, characteristicUuids)
* [fix][android] GetKnownDevice
* [fix][android] bad UUID parsing in ad data for service UUIDs
* [fix][android] multiple notification subscriptions
* [fix][ios] reconnection issues

## 2.0.3
* [fix][android][ios] improved equality checks to help with android events

## 2.0.2
* [fix][android] more gatt133 fixes
* [fix][android] additional fixes for cancel connection
* [fix][android] Connect completion wasn't being called properly

## 2.0.1
* [fix][android] finalization was causing NRE

## 2.0
* [feature] macOS support!
* [feature][all] Connection configuration allows you to set connection priority, notification states on iOS/tvOS/macOS, and whether or not to make the connection persistent
* [feature][macos/tvos/ios] Background mode via CBCentralInitOptions - On the platform project use BleAdapter.Init(BleAdapterConfiguration)
* [feature][ios] Background - Adapter.WhenDeviceStateRestored() will allow to hook for background state restoration (must be used in conjunction with BleAdapter.Init)
* [feature][uwp][droid] Reliable write transaction via Device.BeginReliableWriteTransaction() and GattReliableWriteTransaction
* [feature][uwp][droid] WriteBlob now uses reliable write transactions
* [feature] Device.GetService(Guid[]) and Service.GetCharacteristic(Guid[]) optimized calls
* [feature] Adapter.GetKnownDevice(Guid) - explanation in the signature :)
* [feature] Adapter.GetPairedDevices() - pretty self explanatory
* [breaking][feature] RequestMtu now returns as an observable with what the accepted MTU was
* [breaking] CreateConnection is gone - created more issues than it solved - Use Connect() as it creates persistent connections out of the gate
* [breaking] Disconnect has been renamed to CancelConnection as it cancels any pending connections now
* [breaking] BleAdapter has been renamed to CrossBleAdapter
* [breaking] Acr.Ble namespace has been renamed to Plugin.BluetoothLE
* [fix][droid] disconnect on existing connection tries
* [fix][droid] more gatt 133 issues
* [fix][all] Blob write observable subscriptions not firing properly
* [fix][all] NotifyEncryptionRequired, Indicate, and IndicateEncryptionRequired return true for CanNotify

## 1.3
* [fix][droid] descriptors and characteristic read/writes now adhere to AndroidConfig.WriteOnMainThread
* [fix][ios] WhenStatusChanged was causing OnError when a connection failure occurred
* [fix][core] BlobWrite will now use proper MTU
* [breaking][feature][core] Background scan has been replaced.  The normal scan now takes a configuration.
* [feature][core] Get current MTU size
* [feature][droid] monitor MTU changes

## 1.2
* [feature] ability to open bluetooth settings configuration
* [feature] ability to request MTU is now part of device (still only available on droid - but allows for greater flexibility)
* [feature][droid] ability to pair with a device
* [feature][droid] ability to toggle bluetooth adapter status

## 1.1
* [BREAKING] Characteristic/Descriptor Read, Write, and Notification events now return CharacteristicResult that includes the sender characteristic as well as the data
* [fix][droid] Write was not broadcasting completion at the right time

## 1.0.8
[fix] proper completion of ReadUntil

## 1.0.7
* [feature] IGattCharacteristic.ReadUntil(endBytes) extension method will read in a loop until end bytes detected
* [feature][droid] AndroidConfig.MaxTransmissionUnitSize (MTU) can now be set to negotiate MTU upon connections

## 1.0.6
* [fix][droid] write on main thread (can use AndroidConfig.WriteOnMainThread = false, to disable)
* [feature] Blob write
* [feature] Logging now has deviceconnected/devicedisconnected if you wish to monitor just one of the status'

## 1.0.5
* [fix] ability to check for true WriteNoResponse flags
* [fix][droid] ship proper unsubscribe bytes

## 1.0.4
* [fix] logging cleanup
* [feature][core] add DiscoveredServices, DiscoveredCharacteristics, and DiscoveredDescriptors for easy access
* [feature][core] add logging abilities from device reference
* [feature][droid] add improved way to deal with Android connection issues (please read docs under Android Troubleshooting)

## 1.0.3
* [fix][core] logging would not hook properly to existing connected devices
* [fix][droid] deal with gatt error 133 by delaying service discovery post connection
* [workaround] tvOS was having issues. temporarily pulled from nuget

## 1.0.2
* [feature] write without response void method added
* [feature] proper equals check for all ble objects

## 1.0.1
* [fix][all] new adapter scans only clear disconnected devices from cache
* [feature] Adapter.GetConnectedDevices

## 1.0.0
* [fix][droid] WhenStatusChanged firing on subscription and replays properly
* [fix][droid] properly parsing 16 and 32bit UUIDs in advertisement packet

## 0.9.9
*[breaking] WhenActionOccurs renamed to CreateLogger
*[fix] ensure WhenScanStatusChanged() broadcasts its current state on registration
*Logging now returns actual packet received where applicable

## 0.9.8
* adding tvOS libraries to package (NOT TESTED)
* [fix] createconnection properly persists connection now
* [fix] more logging and discovery issues
* [fix][droid] device.readrssi was not working
* [droid] device.whenstatuschanged will now broadcast Connecting/Disconnecting
* [droid] advertisement packet now gets all service UUIDs parsed

## 0.9.7
* [fix] Error notifications on read/writes
* [fix] Make sure to replay last status for connectable observables
* [fix] Service discovery on iOS and Android was not registering subsequent subscriptions properly
* [fix][droid] Read/Write callbacks now passing values back properly
* [breaking] PersistentConnection is now CreateConnection with improvements to status reporting

## 0.9.6
* Vastly improved logging
* Improvements to observable allocations
* Improvements in service discovery

## 0.9.5
* [breaking] Change extension method names

## 0.9.4
* [breaking] Characteristic method WhenNotificationOccurs() is now called WhenNotificationReceived().  It also no longer subscribes to notifications.  Use new method SubscribeToNotifications().  WhenNotificationReceived() is for logging purposes

## 0.9.3
* Add heartrate plugin (extension method)
* Add super logging plugin (extension method)
* Characteristics and Descriptors now have WhenRead/WhenWritten events to monitor calls externally

## 0.9.2
* ScanListen for working with scan results from a background or decoupled component

## 0.9.1
* BackgroundScan added and ScanFilter removed
* Multiple entry points can now hook up to scan, but only one will run (connectable refcount observable)

## 0.9.0
* Initial Public Release
