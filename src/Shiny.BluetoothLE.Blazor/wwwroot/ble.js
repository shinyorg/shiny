var devices = {};
var dotNetRef;
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
                .catch(() => resolve('notsupported'));
        }
    });
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
