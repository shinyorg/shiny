// Shiny.BluetoothLE.Blazor - Web Bluetooth backing for IBleManager / IPeripheral.
//
// Two things shape this file:
//  1. Web Bluetooth has no free-running scan in any shipping browser. navigator.bluetooth.requestLEScan
//     exists only behind #enable-experimental-web-platform-features; requestDevice (a user-gesture
//     chooser) is the universally available path. Both are supported here - see startScan/requestDevice.
//  2. A service is only reachable if it was named in filters or optionalServices at chooser time. There
//     is no "discover everything" after the fact, so GATT calls are resolved against what was declared.

const devices = {};        // deviceId -> BluetoothDevice
const notifyHandlers = {}; // deviceId|service|characteristic -> handler fn
let scanRef;               // .NET ScanCallback for advertisement scanning
let scan;                  // active LEScan


// ---- capability + access -------------------------------------------------------------------------

export function scanSupport() {
    return {
        advertisements: !!(navigator.bluetooth && navigator.bluetooth.requestLEScan),
        chooser: !!(navigator.bluetooth && navigator.bluetooth.requestDevice)
    };
}


export async function requestAccess() {
    if (navigator.bluetooth === undefined)
        return 'notsupported';

    try {
        if (!await navigator.bluetooth.getAvailability())
            return 'notsupported';
    }
    catch (e) {
        // getAvailability is not universally implemented - fall through to the API probe.
    }

    // Either scanning path is enough to be usable; requestDevice is the one that always exists.
    const support = scanSupport();
    if (!support.advertisements && !support.chooser)
        return 'notsupported';

    try {
        const result = await navigator.permissions.query({ name: 'bluetooth' });
        return result.state;
    }
    catch (e) {
        // The Permissions API rarely covers bluetooth; presence of the API is our best signal.
        return 'granted';
    }
}


// ---- scanning ------------------------------------------------------------------------------------

export async function startScan(callbackRef) {
    if (!navigator.bluetooth.requestLEScan)
        throw new Error("navigator.bluetooth.requestLEScan is unavailable. Enable chrome://flags/#enable-experimental-web-platform-features, or use the chooser path (requestDevice).");

    scanRef = callbackRef;
    navigator.bluetooth.addEventListener('advertisementreceived', processScan);
    scan = await navigator.bluetooth.requestLEScan({
        acceptAllAdvertisements: true,
        keepRepeatedDevices: true
    });
}


export function stopScan() {
    try {
        if (scan) {
            scan.stop();
            scan = null;
        }
        navigator.bluetooth.removeEventListener('advertisementreceived', processScan);
        scanRef = null;
    }
    catch (e) {
        console.error('BLE stopScan error', e);
    }
}


// manufacturerData and serviceData arrive as Maps keyed by company id / service UUID, which do not
// survive JSON serialization. Flattened into arrays with the payloads base64'd, the same shape the
// GATT calls already use for binary data. Without this, beacon formats are invisible to Blazor -
// iBeacon lives in manufacturer data and Eddystone in service data.
function advertisementMapToArray(map, keyName) {
    const out = [];
    if (map && typeof map.forEach === 'function') {
        map.forEach((value, key) => {
            const entry = { data: toBase64(value) };
            entry[keyName] = key;
            out.push(entry);
        });
    }
    return out;
}


function processScan(e) {
    devices[e.device.id] = e.device;

    if (scanRef) {
        scanRef.invokeMethodAsync('OnScan', {
            deviceId: e.device.id,
            deviceName: e.device.name,
            txPower: e.txPower,
            rssi: e.rssi,
            uuids: e.uuids,
            manufacturerData: advertisementMapToArray(e.manufacturerData, 'companyId'),
            serviceData: advertisementMapToArray(e.serviceData, 'uuid')
        });
    }
}


// Chooser-based discovery: the user picks one device and it is registered exactly as a scan hit would
// be. serviceUuids doubles as the filter and as optionalServices, because a service that is not declared
// here can never be read later. Returns null when the chooser is dismissed.
export async function requestDevice(serviceUuids) {
    if (!navigator.bluetooth.requestDevice)
        throw new Error("Web Bluetooth is not available in this browser.");

    const services = (serviceUuids || []).map(u => u.toLowerCase());

    let device;
    try {
        device = await navigator.bluetooth.requestDevice(
            services.length
                ? { filters: services.map(s => ({ services: [s] })), optionalServices: services }
                : { acceptAllDevices: true }
        );
    }
    catch (err) {
        if (err && (err.name === 'NotFoundError' || err.name === 'AbortError'))
            return null;
        throw err;
    }

    devices[device.id] = device;
    return {
        deviceId: device.id,
        deviceName: device.name,
        txPower: 0,
        rssi: 0,
        uuids: services
    };
}


// ---- connection ----------------------------------------------------------------------------------

function requireDevice(deviceId) {
    const device = devices[deviceId];
    if (!device)
        throw new Error(`Unknown device '${deviceId}'. Scan or pick it first.`);

    return device;
}


export async function connect(deviceId, callbackRef) {
    const device = requireDevice(deviceId);

    if (device.__shinyDisconnectHandler)
        device.removeEventListener('gattserverdisconnected', device.__shinyDisconnectHandler);

    device.__shinyDisconnectHandler = () => callbackRef.invokeMethodAsync('OnConnectionStateChanged', false);
    device.addEventListener('gattserverdisconnected', device.__shinyDisconnectHandler);

    await device.gatt.connect();
}


export function disconnect(deviceId) {
    const device = devices[deviceId];
    if (!device)
        return;

    if (device.__shinyDisconnectHandler) {
        device.removeEventListener('gattserverdisconnected', device.__shinyDisconnectHandler);
        device.__shinyDisconnectHandler = null;
    }

    if (device.gatt.connected)
        device.gatt.disconnect();
}


export function isConnected(deviceId) {
    const device = devices[deviceId];
    return !!(device && device.gatt.connected);
}


// ---- GATT ----------------------------------------------------------------------------------------

function requireGatt(deviceId) {
    const device = requireDevice(deviceId);
    if (!device.gatt.connected)
        throw new Error('The peripheral is not connected.');

    return device.gatt;
}


function mapProperties(p) {
    // Mirrors Shiny's CharacteristicProperties flags.
    let flags = 0;
    if (p.broadcast) flags |= 1;
    if (p.read) flags |= 2;
    if (p.writeWithoutResponse) flags |= 4;
    if (p.write) flags |= 8;
    if (p.notify) flags |= 16;
    if (p.indicate) flags |= 32;
    if (p.authenticatedSignedWrites) flags |= 64;
    if (p.reliableWrite || p.writableAuxiliaries) flags |= 128;
    return flags;
}


function toBase64(dataView) {
    if (!dataView)
        return '';

    const bytes = dataView.buffer
        ? new Uint8Array(dataView.buffer, dataView.byteOffset, dataView.byteLength)
        : new Uint8Array(dataView);
    let binary = '';
    for (let i = 0; i < bytes.length; i++)
        binary += String.fromCharCode(bytes[i]);

    return btoa(binary);
}


export async function getServices(deviceId) {
    const services = await requireGatt(deviceId).getPrimaryServices();
    return services.map(s => s.uuid);
}


export async function getCharacteristics(deviceId, serviceUuid) {
    const service = await requireGatt(deviceId).getPrimaryService(serviceUuid.toLowerCase());
    const chars = await service.getCharacteristics();
    return chars.map(c => ({
        uuid: c.uuid,
        properties: mapProperties(c.properties)
    }));
}


async function characteristic(deviceId, serviceUuid, characteristicUuid) {
    const service = await requireGatt(deviceId).getPrimaryService(serviceUuid.toLowerCase());
    return await service.getCharacteristic(characteristicUuid.toLowerCase());
}


export async function getCharacteristic(deviceId, serviceUuid, characteristicUuid) {
    const c = await characteristic(deviceId, serviceUuid, characteristicUuid);
    return { uuid: c.uuid, properties: mapProperties(c.properties) };
}


export async function readCharacteristic(deviceId, serviceUuid, characteristicUuid) {
    const c = await characteristic(deviceId, serviceUuid, characteristicUuid);
    return toBase64(await c.readValue());
}


export async function writeCharacteristic(deviceId, serviceUuid, characteristicUuid, data, withResponse) {
    const c = await characteristic(deviceId, serviceUuid, characteristicUuid);

    // Fall back when the requested mode is not offered - cheap peripherals often declare only one.
    if (withResponse) {
        if (c.writeValueWithResponse) await c.writeValueWithResponse(data);
        else await c.writeValue(data);
    }
    else {
        if (c.writeValueWithoutResponse && c.properties.writeWithoutResponse) await c.writeValueWithoutResponse(data);
        else if (c.writeValueWithResponse) await c.writeValueWithResponse(data);
        else await c.writeValue(data);
    }
}


export async function startNotifications(deviceId, serviceUuid, characteristicUuid, callbackRef) {
    const c = await characteristic(deviceId, serviceUuid, characteristicUuid);
    const key = `${deviceId}|${serviceUuid.toLowerCase()}|${characteristicUuid.toLowerCase()}`;

    const handler = e => callbackRef.invokeMethodAsync(
        'OnNotification',
        serviceUuid,
        characteristicUuid,
        toBase64(e.target.value)
    );

    notifyHandlers[key] = { characteristic: c, handler };
    c.addEventListener('characteristicvaluechanged', handler);
    await c.startNotifications();
}


export async function stopNotifications(deviceId, serviceUuid, characteristicUuid) {
    const key = `${deviceId}|${serviceUuid.toLowerCase()}|${characteristicUuid.toLowerCase()}`;
    const entry = notifyHandlers[key];
    if (!entry)
        return;

    delete notifyHandlers[key];
    entry.characteristic.removeEventListener('characteristicvaluechanged', entry.handler);
    try {
        await entry.characteristic.stopNotifications();
    }
    catch (e) {
        // The link may already be gone - nothing left to unsubscribe.
    }
}


export async function getDescriptors(deviceId, serviceUuid, characteristicUuid) {
    const c = await characteristic(deviceId, serviceUuid, characteristicUuid);
    const descriptors = await c.getDescriptors();
    return descriptors.map(d => d.uuid);
}


export async function readDescriptor(deviceId, serviceUuid, characteristicUuid, descriptorUuid) {
    const c = await characteristic(deviceId, serviceUuid, characteristicUuid);
    const d = await c.getDescriptor(descriptorUuid.toLowerCase());
    return toBase64(await d.readValue());
}


export async function writeDescriptor(deviceId, serviceUuid, characteristicUuid, descriptorUuid, data) {
    const c = await characteristic(deviceId, serviceUuid, characteristicUuid);
    const d = await c.getDescriptor(descriptorUuid.toLowerCase());
    await d.writeValue(data);
}
