var devices = {};
var dotNetRef;
var scan;

export async function requestAccess() {
    if (navigator.bluetooth === undefined) {
        return 'notsupported';
    }

    try {
        var available = await navigator.bluetooth.getAvailability();
        if (!available) {
            return 'notsupported';
        }
    }
    catch (e) {
        // getAvailability not supported in all browsers, fall through
    }

    if (!navigator.bluetooth.requestLEScan) {
        return 'notsupported';
    }

    try {
        var result = await navigator.permissions.query({ name: 'bluetooth' });
        return result.state;
    }
    catch (e) {
        // Permissions API for bluetooth not supported in most browsers
        // If navigator.bluetooth exists and requestLEScan is available, treat as granted
        return 'granted';
    }
}

export async function startScan(callbackRef) {
    dotNetRef = callbackRef;
    try {
        navigator.bluetooth.addEventListener('advertisementreceived', processScan);
        scan = await navigator.bluetooth.requestLEScan({
            acceptAllAdvertisements: true,
            keepRepeatedDevices: true
        });
    }
    catch (e) {
        console.error('BLE startScan error', e);
        throw e;
    }
}

export function stopScan() {
    try {
        if (scan) {
            scan.stop();
            scan = null;
        }
        navigator.bluetooth.removeEventListener('advertisementreceived', processScan);
        dotNetRef = null;
    }
    catch (e) {
        console.error('BLE stopScan error', e);
    }
}

function processScan(e) {
    devices[e.device.id] = e.device;

    if (dotNetRef) {
        dotNetRef.invokeMethodAsync('OnScan', {
            deviceId: e.device.id,
            deviceName: e.device.name,
            txPower: e.txPower,
            rssi: e.rssi,
            uuids: e.uuids
        });
    }
}
