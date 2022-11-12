var devices;
var objRef;
var scan;

export function requestAccess() {
    return new Promise((resolve, reject) => {
        if (navigator.bluetooth === undefined) {
            resolve('notsupported');
        }
        else {
            navigator
                .permissions
                .query({ name: 'bluetooth' })
                .then(result => resolve(result.state))
                .catch(() => resolve('notsupported')); // permission does not exist
        }
    });
}


//chrome://flags/#enable-experimental-web-platform-features
export async function startScan(dotNetRef) {
    objRef = dotNetRef;
    try {
        navigator.bluetooth.addEventListener('advertisementreceived', processScan);
        scan = await navigator.bluetooth.requestLEScan({
            acceptAllAdvertisements: true,
            keepRepeatedDevices: true
        });        
    }
    catch (e) {
        console.log('error', e);
    }
}

export function stopScan() {
    try {
        scan.stop();
        navigator.bluetooth.removeEventListener('advertisementreceived', processScan);
        objRef = null;
    }
    catch (e) {
        console.log('stopScan', e);
    }
}

//export function connect(deviceId) {

//}

//export function disconnect(deviceId) {

//}

//export function readServices(deviceId) {

//}

//export function readCharacterisitics(deviceId, serviceId) {

//}

//export function readDescriptors(deviceId, serviceId, characteristicId) {

//}

//export function readCharacteristic(deviceId, serviceId, characteristicId) {

//}

//export function writeCharacteristic(deviceId, serviceId, characteristicId, data) {

//}

//export function subscribeCharacteristic(dotNetRef, deviceId, serviceId, characteristicId) {

//}

//export function unsubscribeCharacteristic(deviceId, serviceId, characteristicId) {
//}

export function processScan(e) {
    console.log('AD RECEIVED', e);
    devices[e.device.id] = e.device;
    console.logs('BLUETOOTH', devices);
    console.logs('BT AD', e);

    //log('Advertisement received.');
    //log('  Device Name: ' + event.device.name);
    //log('  Device ID: ' + event.device.id);
    //log('  RSSI: ' + event.rssi);
    //log('  TX Power: ' + event.txPower);
    //log('  UUIDs: ' + event.uuids);
    //event.manufacturerData.forEach((valueDataView, key) => {
    //    logDataView('Manufacturer', key, valueDataView);
    //});
    //event.serviceData.forEach((valueDataView, key) => {
    //    logDataView('Service', key, valueDataView);
    //});

    objRef.invokeMethod('OnScan', {
        deviceId: e.device.id,
        deviceName: e.device.name,
        txPower: e.txPower,
        rssi: e.rssi,
        uuids: e.uuids
    });
}


//function recordNearbyBeacon(major, minor, pathLossVs1m) { ... }
//navigator.bluetooth.requestLEScan({
//    filters: [{
//        manufacturerData: {
//            0x004C: {
//                dataPrefix: new Uint8Array([
//                    0x02, 0x15, // iBeacon identifier.
//                    0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15  // My beacon UUID.
//                ])
//            }
//        }
//    }],
//    keepRepeatedDevices: true
//}).then(() => {
//    navigator.bluetooth.addEventListener('advertisementreceived', event => {
//        let appleData = event.manufacturerData.get(0x004C);
//        if (appleData.byteLength != 23) {
//            // Isn’t an iBeacon.
//            return;
//        }
//        let major = appleData.getUint16(18, false);
//        let minor = appleData.getUint16(20, false);
//        let txPowerAt1m = -appleData.getInt8(22);
//        let pathLossVs1m = txPowerAt1m - event.rssi;

//        recordNearbyBeacon(major, minor, pathLossVs1m);
//    });
//})