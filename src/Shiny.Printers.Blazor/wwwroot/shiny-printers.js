// Shiny.Printers.Blazor - browser transports for the thermal ESC/POS byte stream.
//
// Each requestXxx() returns a "connection" object exposing the same three members, so the .NET side
// (BrowserPrinterConnection) stays transport-agnostic:
//   describe()      -> { transport, name, id, detail }
//   write(bytes)    -> chunks a Uint8Array out to the device
//   disconnect()    -> tears the transport down
//
// Returning null instead of a connection means the user dismissed the browser's device picker.

export function getSupport() {
    const nav = typeof navigator === "undefined" ? null : navigator;
    return {
        bluetooth: !!(nav && nav.bluetooth),
        serial: !!(nav && nav.serial),
        usb: !!(nav && nav.usb),
        secureContext: typeof window !== "undefined" && !!window.isSecureContext
    };
}

// The picker throws NotFoundError when dismissed, AbortError if it is torn down programmatically.
function isPickerCancel(err) {
    return !!err && (err.name === "NotFoundError" || err.name === "AbortError");
}

function delay(ms) {
    return ms > 0 ? new Promise(resolve => setTimeout(resolve, ms)) : Promise.resolve();
}

// Cheap thermal printers have tiny receive buffers, so every transport writes in bounded chunks with
// an optional drain pause. slice() (not subarray) so a transport that retains the buffer can't see
// later chunks mutate underneath it.
async function writeChunked(bytes, chunkSize, interChunkDelayMs, writeChunk) {
    const size = chunkSize > 0 ? chunkSize : 180;
    for (let offset = 0; offset < bytes.length; offset += size) {
        const end = Math.min(offset + size, bytes.length);
        await writeChunk(bytes.slice(offset, end));
        if (end < bytes.length)
            await delay(interChunkDelayMs);
    }
}


// ---- Web Bluetooth -------------------------------------------------------------------------------

export async function requestBluetooth(config, dotNetRef) {
    if (!navigator.bluetooth)
        throw new Error("Web Bluetooth is not available in this browser.");

    const services = config.profiles.map(p => p.service.toLowerCase());

    let device;
    try {
        device = await navigator.bluetooth.requestDevice(
            config.acceptAllDevices
                ? { acceptAllDevices: true, optionalServices: services }
                : { filters: services.map(s => ({ services: [s] })), optionalServices: services }
        );
    }
    catch (err) {
        if (isPickerCancel(err)) return null;
        throw err;
    }

    const server = await device.gatt.connect();

    // The picker never reports which filter matched, so probe the known profiles in priority order.
    let characteristic = null;
    let matched = null;
    for (const profile of config.profiles) {
        try {
            const service = await server.getPrimaryService(profile.service.toLowerCase());
            characteristic = profile.write
                ? await service.getCharacteristic(profile.write.toLowerCase())
                : (await service.getCharacteristics()).find(c => c.properties.write || c.properties.writeWithoutResponse);

            if (characteristic) {
                matched = profile;
                break;
            }
        }
        catch {
            // Not this profile - keep probing.
        }
    }

    if (!characteristic) {
        device.gatt.disconnect();
        throw new Error("Connected, but found no known ESC/POS write characteristic on this device.");
    }

    const noResponse = config.writeWithoutResponse && characteristic.properties.writeWithoutResponse;
    const writeChunk = chunk => {
        if (noResponse)
            return characteristic.writeValueWithoutResponse ? characteristic.writeValueWithoutResponse(chunk) : characteristic.writeValue(chunk);
        return characteristic.writeValueWithResponse ? characteristic.writeValueWithResponse(chunk) : characteristic.writeValue(chunk);
    };

    const onDisconnected = () => dotNetRef.invokeMethodAsync("OnDisconnected");
    device.addEventListener("gattserverdisconnected", onDisconnected);

    return {
        describe: () => ({
            transport: "WebBluetooth",
            name: device.name || "Bluetooth printer",
            id: device.id || "",
            detail: matched.name
        }),
        write: bytes => writeChunked(bytes, config.chunkSize, config.interChunkDelayMs, writeChunk),
        disconnect: () => {
            device.removeEventListener("gattserverdisconnected", onDisconnected);
            if (device.gatt.connected)
                device.gatt.disconnect();
        }
    };
}


// ---- Web Serial ----------------------------------------------------------------------------------

export async function requestSerial(config, dotNetRef) {
    if (!navigator.serial)
        throw new Error("Web Serial is not available in this browser.");

    let port;
    try {
        const filters = (config.usbVendorIds || []).map(id => ({ usbVendorId: id }));
        port = await navigator.serial.requestPort(filters.length ? { filters } : {});
    }
    catch (err) {
        if (isPickerCancel(err)) return null;
        throw err;
    }

    const open = {
        baudRate: config.baudRate,
        dataBits: config.dataBits,
        stopBits: config.stopBits,
        parity: config.parity,
        flowControl: config.flowControl
    };
    if (config.bufferSize > 0)
        open.bufferSize = config.bufferSize;

    await port.open(open);

    const writer = port.writable.getWriter();
    const onDisconnect = () => dotNetRef.invokeMethodAsync("OnDisconnected");
    port.addEventListener("disconnect", onDisconnect);

    const info = port.getInfo ? port.getInfo() : {};

    return {
        describe: () => ({
            transport: "WebSerial",
            name: "Serial printer",
            id: info.usbVendorId != null ? `${info.usbVendorId}:${info.usbProductId}` : "",
            detail: `${config.baudRate} baud ${config.dataBits}${config.parity[0].toUpperCase()}${config.stopBits}`
        }),
        write: bytes => writeChunked(bytes, config.chunkSize, config.interChunkDelayMs, chunk => writer.write(chunk)),
        disconnect: async () => {
            port.removeEventListener("disconnect", onDisconnect);
            // close() releases the lock; releaseLock() afterwards is a no-op that may throw.
            try { await writer.close(); } catch { /* already closed */ }
            try { writer.releaseLock(); } catch { /* already released */ }
            try { await port.close(); } catch { /* already closed */ }
        }
    };
}


// ---- WebUSB --------------------------------------------------------------------------------------

const UsbPrinterClass = 7;

export async function requestUsb(config, dotNetRef) {
    if (!navigator.usb)
        throw new Error("WebUSB is not available in this browser.");

    let device;
    try {
        const filters = (config.filters || []).map(f => {
            const filter = {};
            if (f.vendorId != null) filter.vendorId = f.vendorId;
            if (f.productId != null) filter.productId = f.productId;
            if (f.classCode != null) filter.classCode = f.classCode;
            return filter;
        });
        device = await navigator.usb.requestDevice({ filters: filters.length ? filters : [{ classCode: UsbPrinterClass }] });
    }
    catch (err) {
        if (isPickerCancel(err)) return null;
        throw err;
    }

    await device.open();
    if (!device.configuration)
        await device.selectConfiguration(1);

    // Any bulk OUT endpoint can carry the byte stream; prefer one on a real USB printer-class interface.
    const candidates = [];
    for (const iface of device.configuration.interfaces) {
        for (const alt of iface.alternates) {
            const out = alt.endpoints.find(e => e.direction === "out" && e.type === "bulk");
            if (out)
                candidates.push({ iface, alt, out });
        }
    }

    const target = candidates.find(c => c.alt.interfaceClass === UsbPrinterClass) || candidates[0];
    if (!target) {
        await device.close();
        throw new Error("No bulk OUT endpoint found on this USB device.");
    }

    try {
        await device.claimInterface(target.iface.interfaceNumber);
    }
    catch (err) {
        await device.close();
        throw new Error(
            `Could not claim USB interface ${target.iface.interfaceNumber} (${err.message}). ` +
            "The OS printer driver usually owns this device - remove it from the system printer list, " +
            "or use Web Serial instead."
        );
    }

    if (target.alt.alternateSetting !== 0)
        await device.selectAlternateInterface(target.iface.interfaceNumber, target.alt.alternateSetting);

    const endpoint = target.out.endpointNumber;
    const onDisconnect = e => {
        if (e.device === device)
            dotNetRef.invokeMethodAsync("OnDisconnected");
    };
    navigator.usb.addEventListener("disconnect", onDisconnect);

    return {
        describe: () => ({
            transport: "WebUsb",
            name: device.productName || "USB printer",
            id: `${device.vendorId}:${device.productId}`,
            detail: `interface ${target.iface.interfaceNumber} endpoint ${endpoint}`
        }),
        write: bytes => writeChunked(bytes, config.chunkSize, config.interChunkDelayMs, async chunk => {
            const result = await device.transferOut(endpoint, chunk);
            if (result.status !== "ok")
                throw new Error(`USB transfer failed: ${result.status}`);
        }),
        disconnect: async () => {
            navigator.usb.removeEventListener("disconnect", onDisconnect);
            try { await device.releaseInterface(target.iface.interfaceNumber); } catch { /* device already gone */ }
            try { await device.close(); } catch { /* already closed */ }
        }
    };
}
